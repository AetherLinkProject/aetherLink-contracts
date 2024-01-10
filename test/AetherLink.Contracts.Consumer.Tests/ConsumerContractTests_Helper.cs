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

namespace AetherLink.Contracts.Consumer;

public partial class ConsumerContractTests
{
    private async Task InitializeAsync()
    {
        var result = await ConsumerContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var output = await ConsumerContractStub.GetAdmin.CallAsync(new Empty());
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

    private ByteString GenerateSignature(byte[] privateKey, TransmitInput input)
    {
        var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input.Report.ToByteArray()),
            HashHelper.ComputeFrom(input.ReportContext.ToString()));
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, hash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }

    private async Task<(long, int)> PrepareForVrfWithoutUpdateContractsAsync()
    {
        long subscriptionId;
        int requestTypeIndex;

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
            var result = await OracleContractStub.AddCoordinator.SendAsync(VrfCoordinatorContractAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<CoordinatorSet>(result.TransactionResult);
            requestTypeIndex = log.RequestTypeIndex;
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
        {
            var result = await OracleContractStub.RegisterProvingKey.SendAsync(new RegisterProvingKeyInput
            {
                Oracle = DefaultAddress,
                PublicProvingKey = DefaultKeyPair.PublicKey.ToHex()
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var result = await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(ConsumerContractAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var log = GetLogEvent<SubscriptionCreated>(result.TransactionResult);
            subscriptionId = log.SubscriptionId;
        }

        // prepare VRF Coordinator contract
        {
            var result = await VrfContractStub.Initialize.SendAsync(new Coordinator.InitializeInput
            {
                Admin = DefaultAddress,
                Oracle = OracleContractAddress,
                RequestTypeIndex = requestTypeIndex
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // prepare Consumer contract
        {
            var result = await ConsumerContractStub.Initialize.SendAsync(new InitializeInput
            {
                Admin = DefaultAddress,
                Oracle = OracleContractAddress,
                VrfRequestTypeIndex = requestTypeIndex
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        return (subscriptionId, requestTypeIndex);
    }

    private async Task<(long, int)> PrepareForDataFeedsWithoutUpdateContractsAsync()
    {
        long subscriptionId;
        int requestTypeIndex;

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
            var result = await OracleContractStub.AddCoordinator.SendAsync(DataFeedsCoordinatorContractAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<CoordinatorSet>(result.TransactionResult);
            requestTypeIndex = log.RequestTypeIndex;
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
        {
            var result = await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(ConsumerContractAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionCreated>(result.TransactionResult);
            subscriptionId = log.SubscriptionId;
        }

        // prepare DataFeeds Coordinator contract
        {
            var result = await DataFeedsContractStub.Initialize.SendAsync(new Coordinator.InitializeInput
            {
                Admin = DefaultAddress,
                Oracle = OracleContractAddress,
                RequestTypeIndex = requestTypeIndex
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // prepare Consumer contract
        {
            var result = await ConsumerContractStub.Initialize.SendAsync(new InitializeInput
            {
                Admin = DefaultAddress,
                Oracle = OracleContractAddress,
                DataFeedsRequestTypeIndex = requestTypeIndex
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        return (subscriptionId, requestTypeIndex);
    }
}