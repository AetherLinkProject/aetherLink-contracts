using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.CSharp.Core;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AetherLink.Contracts.Ramp;

public partial class RampContractTests
{
    private async Task InitializeAsync()
    {
        var result = await RampContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var output = await RampContractStub.GetAdmin.CallAsync(new Empty());
        output.ShouldBe(DefaultAddress);
    }

    private T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }

    private ByteString GenerateSignature(byte[] privateKey, CommitInput input)
    {
        var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input.Report.ToByteArray()),
            HashHelper.ComputeFrom(input.Report.ReportContext.ToString()));
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, hash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }

    private async Task PrepareOracleContractsAsync()
    {
        // prepare Oracle contract
        List<Address> signers = new()
            { Signer1Address, Signer2Address, Signer3Address, Signer4Address, Signer5Address };
        List<Address> transmitters = new()
            { Transmitter1Address, Transmitter2Address, Transmitter3Address, Transmitter4Address, DefaultAddress };
        const int f = 1;

        {
            var result = await OracleContractStub.Initialize.SendAsync(new Oracle.InitializeInput
            {
                Admin = DefaultAddress
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var result = await OracleContractStub.SetConfig.SendAsync(new SetConfigInput
            {
                F = f,
                Signers = { signers },
                Transmitters = { transmitters }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // prepare Ramp contract
        {
            var result = await RampContractStub.Initialize.SendAsync(new InitializeInput
            {
                Admin = DefaultAddress,
                Oracle = OracleContractAddress,
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var result = await RampContractStub.SetConfig.SendAsync(new Config
            {
                ChainIdList = new ChainIdList { Data = { 1, 56, 1100, 1866392, 1931928, 9992731 } }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var result = await RampContractStub.AddRampSender.SendAsync(new AddRampSenderInput
            {
                SenderAddress = UserAddress
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}