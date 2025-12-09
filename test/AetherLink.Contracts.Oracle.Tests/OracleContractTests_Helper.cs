using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContractTests
{
    private async Task InitializeAsync()
    {
        var result = await OracleContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
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

    private async Task PauseAsync()
    {
        var result = await OracleContractStub.Pause.SendAsync(new Empty());
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }
}