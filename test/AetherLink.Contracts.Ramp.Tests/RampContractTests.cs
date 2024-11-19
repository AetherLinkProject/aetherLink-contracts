using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AetherLink.Contracts.Ramp;

public partial class RampContractTests : RampContractTestBase
{
    [Fact]
    public async Task InitializeTests()
    {
        {
            var result = await RampContractStub.Initialize.SendAsync(new InitializeInput
            {
                Admin = DefaultAddress,
                Oracle = UserAddress
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var output = await RampContractStub.GetOracleContractAddress.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }
        {
            var output = await RampContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
    }

    [Fact]
    public async Task InitializeTests_Fail()
    {
        {
            var result = await RampContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input admin.");
        }
        {
            var result = await RampContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = DefaultAddress,
                Oracle = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input oracle contract.");
        }

        {
            var result = await UserRampContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("No initialize permission.");
        }

        await RampContractStub.Initialize.SendAsync(new InitializeInput());

        {
            var output = await RampContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var output = await RampContractStub.GetOracleContractAddress.CallAsync(new Empty());
            output.ShouldBe(new Address());
        }
        {
            var result = await RampContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Already initialized.");
        }
    }

    [Fact]
    public async Task SetOracleContractAddressTests()
    {
        await InitializeAsync();

        {
            var result = await RampContractStub.SetOracleContractAddress.SendAsync(UserAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var output = await RampContractStub.GetOracleContractAddress.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }
    }

    [Fact]
    public async Task SetOracleContractAddressTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await UserRampContractStub.SetOracleContractAddress.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await RampContractStub.SetOracleContractAddress.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid Oracle Address input.");
        }
    }

    [Fact]
    public async Task SetConfigTests()
    {
        await InitializeAsync();

        var supportedChainIdList = new ChainIdList { Data = { 1 } };
        var result = await RampContractStub.SetConfig.SendAsync(new Config { ChainIdList = supportedChainIdList });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<ConfigSet>(result.TransactionResult);
        log.Config.ChainIdList.ShouldBe(supportedChainIdList);

        var output = await RampContractStub.GetConfig.CallAsync(new Empty());
        output.ChainIdList.ShouldBe(supportedChainIdList);
    }

    [Fact]
    public async Task SetConfigTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await UserRampContractStub.SetConfig.SendWithExceptionAsync(new Config());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await RampContractStub.SetConfig.SendWithExceptionAsync(new Config());
            result.TransactionResult.Error.ShouldContain("Invalid input chain id list.");
        }
        {
            var result = await RampContractStub.SetConfig.SendWithExceptionAsync(new Config
            {
                ChainIdList = new ChainIdList()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input chain id list.");
        }
    }

    [Fact]
    public async Task AddRampSenderTests()
    {
        await InitializeAsync();

        var result = await RampContractStub.AddRampSender.SendAsync(new() { SenderAddress = UserAddress });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<RampSenderAdded>(result.TransactionResult);
        log.SenderAddress.ShouldBe(UserAddress);

        var output = await RampContractStub.GetRampSender.CallAsync(UserAddress);
        output.SenderAddress.ShouldBe(UserAddress);
    }

    [Fact]
    public async Task AddRampSender_Fail()
    {
        await InitializeAsync();

        {
            var result =
                await UserRampContractStub.AddRampSender.SendWithExceptionAsync(new() { SenderAddress = UserAddress });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await RampContractStub.AddRampSender.SendWithExceptionAsync(new());
            result.TransactionResult.Error.ShouldContain("Invalid ramp sender address.");
        }
        {
            await RampContractStub.AddRampSender.SendAsync(new() { SenderAddress = UserAddress });
            var result =
                await RampContractStub.AddRampSender.SendWithExceptionAsync(new() { SenderAddress = UserAddress });
            result.TransactionResult.Error.ShouldContain("Sender was existed.");
        }
    }

    [Fact]
    public async Task RemoveRampSenderTests()
    {
        await InitializeAsync();

        await RampContractStub.AddRampSender.SendAsync(new() { SenderAddress = UserAddress });

        var result = await RampContractStub.RemoveRampSender.SendAsync(UserAddress);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var log = GetLogEvent<RampSenderRemoved>(result.TransactionResult);
        log.SenderAddress.ShouldBe(UserAddress);

        var output = await RampContractStub.GetRampSender.CallAsync(UserAddress);
        output.SenderAddress.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveRampSender_Fail()
    {
        await InitializeAsync();

        {
            var result = await UserRampContractStub.RemoveRampSender.SendWithExceptionAsync(new());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await RampContractStub.RemoveRampSender.SendWithExceptionAsync(new());
            result.TransactionResult.Error.ShouldContain("Invalid sender address to remove.");
        }
        {
            var result = await RampContractStub.RemoveRampSender.SendWithExceptionAsync(UserAddress);
            result.TransactionResult.Error.ShouldContain("Sender is not existed.");
        }
    }

    [Fact]
    public async Task SetTokenSwapConfigTests()
    {
        await InitializeAsync();

        {
            var result = await RampContractStub.AddRampSender.SendAsync(new() { SenderAddress = UserAddress });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var tokenSwapConfigs = new TokenSwapList();
            var tonTokenSwapInfoList = new TokenSwapInfo
            {
                SwapId = "123",
                TargetChainId = 1100,
                TargetContractAddress = "TON",
                TokenAddress = "TON",
                OriginToken = "ELFTON"
            };
            tokenSwapConfigs.TokenSwapInfoList.Add(tonTokenSwapInfoList);

            var evmTokenSwapInfoList = new TokenSwapInfo
            {
                SwapId = "123",
                TargetChainId = 100,
                TargetContractAddress = "EVM",
                TokenAddress = "EVM",
                OriginToken = "ELFETH"
            };
            tokenSwapConfigs.TokenSwapInfoList.Add(evmTokenSwapInfoList);

            var result =
                await UserRampContractStub.SetTokenSwapConfig.SendAsync(new() { TokenSwapList = tokenSwapConfigs });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<TokenSwapConfigUpdated>(result.TransactionResult);
            log.TokenSwapList.ShouldBe(tokenSwapConfigs);

            var tokenSwapConfig = await UserRampContractStub.GetTokenSwapConfig.CallAsync(UserAddress);
            tokenSwapConfig.TokenSwapList.TokenSwapInfoList.ShouldBe(tokenSwapConfigs.TokenSwapInfoList);
        }
    }

    [Fact]
    public async Task SetTokenSwapConfigTests_Failed()
    {
        var configs = new TokenSwapList();
        var tokenSwapInfo = new TokenSwapInfo
        {
            SwapId = "123",
            TargetChainId = 1,
            TargetContractAddress = "ABC",
            TokenAddress = "ABC",
            OriginToken = "ELF"
        };
        configs.TokenSwapInfoList.Add(tokenSwapInfo);

        var result = await UserRampContractStub.SetTokenSwapConfig.SendWithExceptionAsync(new()
            { TokenSwapList = configs });
        result.TransactionResult.Error.ShouldContain("The sender does not have permission to set TokenSwap config.");
    }
}