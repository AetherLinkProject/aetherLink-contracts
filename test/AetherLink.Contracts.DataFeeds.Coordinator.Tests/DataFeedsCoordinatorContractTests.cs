using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Coordinator;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AetherLink.Contracts.DataFeeds.Coordinator;

public partial class DataFeedsCoordinatorContractTests : DataFeedsCoordinatorContractTestBase
{
    [Fact]
    public async Task InitializeTests()
    {
        {
            var result = await CoordinatorContractStub.Initialize.SendAsync(new InitializeInput
            {
                Admin = DefaultAddress,
                Oracle = UserAddress,
                RequestTypeIndex = 1
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var output = await CoordinatorContractStub.GetOracleContractAddress.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }
        {
            var output = await CoordinatorContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var output = await CoordinatorContractStub.GetConfig.CallAsync(new Empty());
            output.RequestTimeoutSeconds.ShouldBe(0);
        }
        {
            var output = await CoordinatorContractStub.GetRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(1);
        }
    }

    [Fact]
    public async Task InitializeTests_Fail()
    {
        {
            var result = await CoordinatorContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input admin.");
        }
        {
            var result = await CoordinatorContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = DefaultAddress,
                Oracle = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input oracle contract.");
        }

        {
            var result = await UserCoordinatorContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await CoordinatorContractStub.Initialize.SendAsync(new InitializeInput());

        {
            var output = await CoordinatorContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var output = await CoordinatorContractStub.GetOracleContractAddress.CallAsync(new Empty());
            output.ShouldBe(new Address());
        }
        {
            var result = await CoordinatorContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Already initialized.");
        }
    }

    [Fact]
    public async Task TransferAdminTests()
    {
        await InitializeAsync();

        {
            var output = await CoordinatorContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var result = await CoordinatorContractStub.TransferAdmin.SendAsync(Accounts[3].Address);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<AdminTransferRequested>(result.TransactionResult);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(Accounts[3].Address);

            var output = await CoordinatorContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var result = await CoordinatorContractStub.TransferAdmin.SendAsync(UserAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<AdminTransferRequested>(result.TransactionResult);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(UserAddress);
        }
        {
            var result = await UserCoordinatorContractStub.AcceptAdmin.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<AdminTransferred>(result.TransactionResult);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(UserAddress);

            var output = await CoordinatorContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }
    }

    [Fact]
    public async Task TransferAdminTests_Fail()
    {
        {
            var result = await CoordinatorContractStub.TransferAdmin.SendWithExceptionAsync(UserAddress);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await UserCoordinatorContractStub.AcceptAdmin.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await InitializeAsync();

        {
            var result = await CoordinatorContractStub.TransferAdmin.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input admin.");
        }
        {
            var result = await UserCoordinatorContractStub.TransferAdmin.SendWithExceptionAsync(UserAddress);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await CoordinatorContractStub.TransferAdmin.SendWithExceptionAsync(DefaultAddress);
            result.TransactionResult.Error.ShouldContain("Cannot transfer to self.");
        }

        await CoordinatorContractStub.TransferAdmin.SendAsync(UserAddress);

        {
            var result = await CoordinatorContractStub.AcceptAdmin.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await UserCoordinatorContractStub.AcceptAdmin.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            result = await UserCoordinatorContractStub.AcceptAdmin.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
    }

    [Fact]
    public async Task SetOracleContractAddressTests()
    {
        await InitializeAsync();

        {
            var result = await CoordinatorContractStub.SetOracleContractAddress.SendAsync(UserAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var output = await CoordinatorContractStub.GetOracleContractAddress.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }
    }

    [Fact]
    public async Task SetOracleContractAddressTests_Fail()
    {
        await InitializeAsync();

        {
            var result =
                await UserCoordinatorContractStub.SetOracleContractAddress.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await CoordinatorContractStub.SetOracleContractAddress.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetConfigTests()
    {
        const int requestTimeoutSeconds = 10;

        await InitializeAsync();

        {
            var result = await CoordinatorContractStub.SetConfig.SendAsync(new Config
            {
                RequestTimeoutSeconds = requestTimeoutSeconds
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<ConfigSet>(result.TransactionResult);
            log.Config.RequestTimeoutSeconds.ShouldBe(requestTimeoutSeconds);

            var output = await CoordinatorContractStub.GetConfig.CallAsync(new Empty());
            output.RequestTimeoutSeconds.ShouldBe(requestTimeoutSeconds);
        }
        {
            var result = await CoordinatorContractStub.SetConfig.SendAsync(new Config
            {
                RequestTimeoutSeconds = requestTimeoutSeconds
            });

            var output = await CoordinatorContractStub.GetConfig.CallAsync(new Empty());
            output.RequestTimeoutSeconds.ShouldBe(requestTimeoutSeconds);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(ConfigSet)));
            log.ShouldBeNull();
        }
    }

    [Fact]
    public async Task SetConfigTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await UserCoordinatorContractStub.SetConfig.SendWithExceptionAsync(new Config());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await CoordinatorContractStub.SetConfig.SendWithExceptionAsync(new Config
            {
                RequestTimeoutSeconds = -1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input request timeout seconds.");
        }
    }

    [Fact]
    public async Task PauseTests()
    {
        await InitializeAsync();

        {
            var result = await CoordinatorContractStub.Pause.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<Paused>(result.TransactionResult);
            log.Account.ShouldBe(DefaultAddress);
        }
        {
            var output = await CoordinatorContractStub.IsPaused.CallAsync(new Empty());
            output.Value.ShouldBeTrue();
        }
        {
            var result = await CoordinatorContractStub.Unpause.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<Unpaused>(result.TransactionResult);
            log.Account.ShouldBe(DefaultAddress);
        }
        {
            var output = await CoordinatorContractStub.IsPaused.CallAsync(new Empty());
            output.Value.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task PauseTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await CoordinatorContractStub.Unpause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Contract not on pause.");
        }
        {
            var result = await UserCoordinatorContractStub.Pause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await CoordinatorContractStub.Pause.SendAsync(new Empty());

        {
            var result = await CoordinatorContractStub.Pause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Already paused.");
        }
        {
            var result = await UserCoordinatorContractStub.Unpause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
    }

    [Fact]
    public async Task SetRequestTypeIndexTests()
    {
        await InitializeAsync();

        {
            var result = await CoordinatorContractStub.SetRequestTypeIndex.SendAsync(new Int32Value
            {
                Value = 2
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<RequestTypeIndexSet>(result.TransactionResult);
            log.RequestTypeIndex.ShouldBe(2);

            var output = await CoordinatorContractStub.GetRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(2);
        }
        {
            var result = await CoordinatorContractStub.SetRequestTypeIndex.SendAsync(new Int32Value
            {
                Value = 2
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(RequestTypeIndexSet)));
            log.ShouldBeNull();
        }
    }

    [Fact]
    public async Task SetRequestTypeIndexTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await UserCoordinatorContractStub.SetRequestTypeIndex.SendWithExceptionAsync(new Int32Value());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await CoordinatorContractStub.SetRequestTypeIndex.SendWithExceptionAsync(new Int32Value
            {
                Value = 0
            });
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var result = await CoordinatorContractStub.SetRequestTypeIndex.SendWithExceptionAsync(new Int32Value
            {
                Value = -1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }
}