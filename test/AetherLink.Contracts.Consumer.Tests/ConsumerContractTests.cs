using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AetherLink.Contracts.Consumer;

public partial class ConsumerContractTests : ConsumerContractTestBase
{
    [Fact]
    public async Task InitializeTests()
    {
        {
            var result = await ConsumerContractStub.Initialize.SendAsync(new InitializeInput
            {
                Admin = UserAddress,
                Oracle = OracleContractAddress,
                DataFeedsRequestTypeIndex = 1,
                VrfRequestTypeIndex = 2
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var output = await ConsumerContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }
        {
            var output = await ConsumerContractStub.GetOracleContract.CallAsync(new Empty());
            output.ShouldBe(OracleContractAddress);
        }
        {
            var output = await ConsumerContractStub.GetDataFeedsRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(1);
        }
        {
            var output = await ConsumerContractStub.GetVrfRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(2);
        }
        {
            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(1);
            output.Data.ShouldBe(new List<Address> { UserAddress });
        }
    }

    [Fact]
    public async Task InitializeTests_Fail()
    {
        {
            var result = await UserConsumerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await ConsumerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input admin.");
        }

        await ConsumerContractStub.Initialize.SendAsync(new InitializeInput());

        {
            var output = await ConsumerContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var output = await ConsumerContractStub.GetOracleContract.CallAsync(new Empty());
            output.ShouldBe(new Address());
        }
        {
            var output = await ConsumerContractStub.GetDataFeedsRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(0);
        }
        {
            var output = await ConsumerContractStub.GetVrfRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(0);
        }
        {
            var result = await ConsumerContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
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
            var output = await ConsumerContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var result = await ConsumerContractStub.TransferAdmin.SendAsync(Signer1Address);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var output = await ConsumerContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);

            var log = GetLogEvent<AdminTransferRequested>(result.TransactionResult);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(Signer1Address);
        }
        {
            var result = await ConsumerContractStub.TransferAdmin.SendAsync(UserAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var output = await ConsumerContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);

            var log = GetLogEvent<AdminTransferRequested>(result.TransactionResult);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(UserAddress);
        }
        {
            var result = await UserConsumerContractStub.AcceptAdmin.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var output = await ConsumerContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(UserAddress);

            var log = GetLogEvent<AdminTransferred>(result.TransactionResult);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(UserAddress);
        }
    }

    [Fact]
    public async Task TransferAdminTests_Fail()
    {
        {
            var result = await ConsumerContractStub.TransferAdmin.SendWithExceptionAsync(UserAddress);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await UserConsumerContractStub.AcceptAdmin.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await InitializeAsync();

        {
            var result = await ConsumerContractStub.TransferAdmin.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input admin.");
        }
        {
            var result = await UserConsumerContractStub.TransferAdmin.SendWithExceptionAsync(UserAddress);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await ConsumerContractStub.TransferAdmin.SendWithExceptionAsync(DefaultAddress);
            result.TransactionResult.Error.ShouldContain("Cannot transfer to self.");
        }

        await ConsumerContractStub.TransferAdmin.SendAsync(UserAddress);

        {
            var result = await ConsumerContractStub.AcceptAdmin.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
    }

    [Fact]
    public async Task SetOracleContractAddressTests()
    {
        await InitializeAsync();

        {
            var result = await ConsumerContractStub.SetOracleContract.SendAsync(UserAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var output = await ConsumerContractStub.GetOracleContract.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }
    }

    [Fact]
    public async Task SetOracleContractAddressTests_Fail()
    {
        await InitializeAsync();

        {
            var result =
                await UserConsumerContractStub.SetOracleContract.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await ConsumerContractStub.SetOracleContract.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task AddControllerTests()
    {
        await InitializeAsync();

        {
            var result = await ConsumerContractStub.AddController.SendAsync(new AddressList
            {
                Data = { UserAddress }
            });

            var log = GetLogEvent<ControllerAdded>(result.TransactionResult);
            log.Controllers.Data.Count.ShouldBe(1);
            log.Controllers.Data.ShouldBe(new List<Address> { UserAddress });

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(2);
            output.Data.ShouldBe(new List<Address> { DefaultAddress, UserAddress });
        }
        {
            var result = await ConsumerContractStub.AddController.SendAsync(new AddressList
            {
                Data = { Signer1Address, new Address() }
            });

            var log = GetLogEvent<ControllerAdded>(result.TransactionResult);
            log.Controllers.Data.Count.ShouldBe(2);
            log.Controllers.Data.ShouldBe(new List<Address> { Signer1Address, new() });

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(4);
            output.Data.ShouldBe(new List<Address>
                { DefaultAddress, UserAddress, Signer1Address, new() });
        }
        {
            var result = await ConsumerContractStub.AddController.SendAsync(new AddressList
            {
                Data = { Signer2Address, Signer2Address }
            });

            var log = GetLogEvent<ControllerAdded>(result.TransactionResult);
            log.Controllers.Data.Count.ShouldBe(1);
            log.Controllers.Data.ShouldBe(new List<Address> { Signer2Address });

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(5);
            output.Data.ShouldBe(new List<Address>
                { DefaultAddress, UserAddress, Signer1Address, new(), Signer2Address });
        }
        {
            var result = await ConsumerContractStub.AddController.SendAsync(new AddressList
            {
                Data = { Signer2Address }
            });

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(ControllerAdded)));
            log.ShouldBeNull();

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(5);
            output.Data.ShouldBe(new List<Address>
                { DefaultAddress, UserAddress, Signer1Address, new(), Signer2Address });
        }
    }

    [Fact]
    public async Task AddControllerTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await UserConsumerContractStub.AddController.SendWithExceptionAsync(new AddressList
            {
                Data = { UserAddress }
            });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await ConsumerContractStub.AddController.SendWithExceptionAsync(new AddressList());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task RemoveControllerTests()
    {
        await InitializeAsync();

        {
            await ConsumerContractStub.AddController.SendAsync(new AddressList
            {
                Data =
                {
                    UserAddress, Signer1Address, Signer2Address, Signer3Address, Transmitter1Address,
                    Transmitter2Address,
                    Transmitter3Address
                }
            });

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(8);
        }
        {
            var result = await ConsumerContractStub.RemoveController.SendAsync(new AddressList
            {
                Data = { UserAddress }
            });

            var log = GetLogEvent<ControllerRemoved>(result.TransactionResult);
            log.Controllers.Data.Count.ShouldBe(1);
            log.Controllers.Data.ShouldBe(new List<Address> { UserAddress });

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(7);
            output.Data.ShouldBe(new List<Address>
            {
                DefaultAddress, Signer1Address, Signer2Address, Signer3Address, Transmitter1Address,
                Transmitter2Address, Transmitter3Address
            });
        }
        {
            var result = await ConsumerContractStub.RemoveController.SendAsync(new AddressList
            {
                Data = { Signer1Address, Signer2Address, Signer3Address }
            });

            var log = GetLogEvent<ControllerRemoved>(result.TransactionResult);
            log.Controllers.Data.Count.ShouldBe(3);
            log.Controllers.Data.ShouldBe(new List<Address> { Signer1Address, Signer2Address, Signer3Address });

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(4);
            output.Data.ShouldBe(new List<Address>
            {
                DefaultAddress, Transmitter1Address, Transmitter2Address, Transmitter3Address
            });
        }
        {
            var result = await ConsumerContractStub.RemoveController.SendAsync(new AddressList
            {
                Data = { new Address() }
            });

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(ControllerRemoved)));
            log.ShouldBeNull();

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(4);
            output.Data.ShouldBe(new List<Address>
            {
                DefaultAddress, Transmitter1Address, Transmitter2Address, Transmitter3Address
            });
        }
        {
            var result = await ConsumerContractStub.RemoveController.SendAsync(new AddressList
            {
                Data = { Transmitter1Address, Transmitter1Address }
            });

            var log = GetLogEvent<ControllerRemoved>(result.TransactionResult);
            log.Controllers.Data.Count.ShouldBe(1);
            log.Controllers.Data.ShouldBe(new List<Address> { Transmitter1Address });

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(3);
            output.Data.ShouldBe(new List<Address>
            {
                DefaultAddress, Transmitter2Address, Transmitter3Address
            });
        }
        {
            var result = await ConsumerContractStub.RemoveController.SendAsync(new AddressList
            {
                Data = { Transmitter2Address, Transmitter4Address }
            });

            var log = GetLogEvent<ControllerRemoved>(result.TransactionResult);
            log.Controllers.Data.Count.ShouldBe(1);
            log.Controllers.Data.ShouldBe(new List<Address> { Transmitter2Address });

            var output = await ConsumerContractStub.GetController.CallAsync(new Empty());
            output.Data.Count.ShouldBe(2);
            output.Data.ShouldBe(new List<Address>
            {
                DefaultAddress, Transmitter3Address
            });
        }
    }

    [Fact]
    public async Task RemoveControllerTests_Fail()
    {
        await InitializeAsync();

        {
            var result = UserConsumerContractStub.RemoveController.SendWithExceptionAsync(new AddressList
            {
                Data = { UserAddress }
            });
            result.Result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await ConsumerContractStub.RemoveController.SendWithExceptionAsync(new AddressList());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetRequestTypeIndexTests()
    {
        await InitializeAsync();

        {
            var output = await ConsumerContractStub.GetDataFeedsRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(0);
        }
        {
            var output = await ConsumerContractStub.GetVrfRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(0);
        }

        {
            var result = await ConsumerContractStub.SetDataFeedsRequestTypeIndex.SendAsync(new Int32Value
            {
                Value = 1
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var result = await ConsumerContractStub.SetVrfRequestTypeIndex.SendAsync(new Int32Value
            {
                Value = 2
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var output = await ConsumerContractStub.GetDataFeedsRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(1);
        }
        {
            var output = await ConsumerContractStub.GetVrfRequestTypeIndex.CallAsync(new Empty());
            output.Value.ShouldBe(2);
        }
    }

    [Fact]
    public async Task SetRequestTypeIndexTests_Fail()
    {
        await InitializeAsync();

        {
            var result =
                await UserConsumerContractStub.SetDataFeedsRequestTypeIndex.SendWithExceptionAsync(new Int32Value());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await UserConsumerContractStub.SetVrfRequestTypeIndex.SendWithExceptionAsync(new Int32Value());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        {
            var result =
                await ConsumerContractStub.SetDataFeedsRequestTypeIndex.SendWithExceptionAsync(new Int32Value());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var result = await ConsumerContractStub.SetVrfRequestTypeIndex.SendWithExceptionAsync(new Int32Value());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }
}