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
using InitializeInput = Coordinator.InitializeInput;

namespace AetherLink.Contracts.VRF.Coordinator;

public partial class VrfCoordinatorContractTests
{
    private async Task InitializeAsync()
    {
        var result = await CoordinatorContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var output = await CoordinatorContractStub.GetAdmin.CallAsync(new Empty());
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
}