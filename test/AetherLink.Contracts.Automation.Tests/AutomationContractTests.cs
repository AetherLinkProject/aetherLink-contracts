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
using Xunit;
using Paused = Coordinator.Paused;
using Unpaused = Coordinator.Unpaused;

namespace AetherLink.Contracts.Automation;

public partial class AutomationContractTests : AutomationContractTestBase
{
    [Fact]
    public async Task InitializeTests()
    {
        await Initialize();

        var admin = await AutomationContractStub.GetAdmin.CallAsync(new Empty());
        admin.ShouldBe(DefaultAddress);

        // initialize twice
        var result = await AutomationContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        result.TransactionResult.Error.ShouldContain("Already initialized.");
    }

    [Fact]
    public async Task InitializeTests_Fail()
    {
        // empty address
        var result = await AutomationContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
            Admin = new Address(),
            AutomationTypeIndex = 3,
            Oracle = OracleContractAddress,
            SubscriptionId = 0
        });
        result.TransactionResult.Error.ShouldContain("Invalid input admin.");

        // sender != author
        result = await AutomationContractUserStub.Initialize.SendWithExceptionAsync(new InitializeInput
        {
            Admin = UserAddress,
            AutomationTypeIndex = 3,
            Oracle = OracleContractAddress,
            SubscriptionId = 0
        });
        result.TransactionResult.Error.ShouldContain("No permission.");
    }

    [Fact]
    public async Task SetOracleContractAddressTests()
    {
        await Initialize();

        var result = await AutomationContractStub.SetOracleContractAddress.SendAsync(UserAddress);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var output = await AutomationContractStub.GetOracleContractAddress.CallAsync(new Empty());
        output.ShouldBe(UserAddress);
    }

    [Fact]
    public async Task SetOracleContractAddressTests_Fail()
    {
        await Initialize();

        var result = await AutomationContractUserStub.SetOracleContractAddress.SendWithExceptionAsync(new Address());
        result.TransactionResult.Error.ShouldContain("No permission.");
        result = await AutomationContractStub.SetOracleContractAddress.SendWithExceptionAsync(new Address());
        result.TransactionResult.Error.ShouldContain("Invalid input.");
    }

    [Fact]
    public async Task SetRequestTypeIndexTests()
    {
        await Initialize();

        var output = await AutomationContractStub.GetRequestTypeIndex.CallAsync(new Empty());
        output.Value.ShouldBe(3);

        var result = await AutomationContractStub.SetRequestTypeIndex.SendAsync(new Int32Value { Value = 33 });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        output = await AutomationContractStub.GetRequestTypeIndex.CallAsync(new Empty());
        output.Value.ShouldBe(33);
    }

    [Fact]
    public async Task SetRequestTypeIndexTests_Fail()
    {
        await Initialize();

        var result = await AutomationContractUserStub.SetRequestTypeIndex.SendWithExceptionAsync(new Int32Value());
        result.TransactionResult.Error.ShouldContain("No permission.");

        result = await AutomationContractStub.SetRequestTypeIndex.SendWithExceptionAsync(new Int32Value());
        result.TransactionResult.Error.ShouldContain("Invalid input.");
    }

    [Fact]
    public async Task PauseTests()
    {
        await Initialize();

        {
            var result = await AutomationContractStub.Pause.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<Paused>(result.TransactionResult);
            log.Account.ShouldBe(DefaultAddress);
        }
        {
            var output = await AutomationContractStub.IsPaused.CallAsync(new Empty());
            output.Value.ShouldBeTrue();
        }
        {
            var result = await AutomationContractStub.Unpause.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<Unpaused>(result.TransactionResult);
            log.Account.ShouldBe(DefaultAddress);
        }
        {
            var output = await AutomationContractStub.IsPaused.CallAsync(new Empty());
            output.Value.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task PauseTests_Fail()
    {
        await Initialize();

        {
            var result = await AutomationContractStub.Unpause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Contract not on pause.");
        }
        {
            var result = await AutomationContractUserStub.Pause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await AutomationContractStub.Pause.SendAsync(new Empty());

        {
            var result = await AutomationContractStub.Pause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Already paused.");
        }
        {
            var result = await AutomationContractUserStub.Unpause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
    }

    private async Task Initialize() => await AutomationContractStub.Initialize.SendAsync(new InitializeInput
    {
        Admin = DefaultAddress,
        AutomationTypeIndex = 3,
        Oracle = OracleContractAddress,
        SubscriptionId = 0
    });

    private T GetLogEvent<T>(TransactionResult transactionResult) where T : IEvent<T>, new()
    {
        var log = transactionResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        log.ShouldNotBeNull();

        var logEvent = new T();
        logEvent.MergeFrom(log.NonIndexed);

        return logEvent;
    }

    private async Task<(long, int)> PrepareOracleContractsAsync()
    {
        long subscriptionId;
        int requestTypeIndex;

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
            var result = await OracleContractStub.AddCoordinator.SendAsync(AutomationContractAddress);
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
            var result = await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(AutomationContractAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionCreated>(result.TransactionResult);
            subscriptionId = log.SubscriptionId;
        }

        // prepare DataFeeds Coordinator contract
        {
            var result = await AutomationContractStub.Initialize.SendAsync(new InitializeInput
            {
                Admin = DefaultAddress,
                AutomationTypeIndex = requestTypeIndex,
                Oracle = OracleContractAddress,
                SubscriptionId = 1
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        return (subscriptionId, requestTypeIndex);
    }
    
    private ByteString GenerateSignature(byte[] privateKey, TransmitInput input)
    {
        var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input.Report.ToByteArray()),
            HashHelper.ComputeFrom(input.ReportContext.ToString()));
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, hash.ToByteArray());
        return ByteStringHelper.FromHexString(signature.ToHex());
    }
}