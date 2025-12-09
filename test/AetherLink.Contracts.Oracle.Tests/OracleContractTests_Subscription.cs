using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContractTests
{
    [Fact]
    public async Task SetSubscriptionConfigTests()
    {
        const long defaultMaxConsumersPerSubscription = 64;
        const long newMaxConsumersPerSubscription = long.MaxValue;

        await InitializeAsync();

        {
            var output = await OracleContractStub.GetSubscriptionConfig.CallAsync(new Empty());
            output.MaxConsumersPerSubscription.ShouldBe(defaultMaxConsumersPerSubscription);
        }
        {
            var result = await OracleContractStub.SetSubscriptionConfig.SendAsync(new SubscriptionConfig
            {
                MaxConsumersPerSubscription = newMaxConsumersPerSubscription
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionConfigSet>(result.TransactionResult);
            log.Config.MaxConsumersPerSubscription.ShouldBe(newMaxConsumersPerSubscription);

            var output = await OracleContractStub.GetSubscriptionConfig.CallAsync(new Empty());
            output.MaxConsumersPerSubscription.ShouldBe(newMaxConsumersPerSubscription);
        }
        {
            var result = await OracleContractStub.SetSubscriptionConfig.SendAsync(new SubscriptionConfig
            {
                MaxConsumersPerSubscription = newMaxConsumersPerSubscription
            });

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(SubscriptionConfigSet)));
            log.ShouldBeNull();
        }
    }

    [Fact]
    public async Task SetSubscriptionConfigTests_Fail()
    {
        await InitializeAsync();

        {
            var result =
                await UserOracleContractStub.SetSubscriptionConfig.SendWithExceptionAsync(new SubscriptionConfig());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.SetSubscriptionConfig.SendWithExceptionAsync(new SubscriptionConfig
            {
                MaxConsumersPerSubscription = -1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input max consumers per subscription.");
        }
    }

    [Fact]
    public async Task CreateSubscriptionTests()
    {
        const long subscriptionCount = 0;

        await InitializeAsync();

        {
            var output = await OracleContractStub.GetSubscriptionCount.CallAsync(new Empty());
            output.Value.ShouldBe(subscriptionCount);
        }
        {
            var result = await OracleContractStub.CreateSubscription.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionCreated>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(subscriptionCount + 1);
            log.Owner.ShouldBe(DefaultAddress);

            var output = await OracleContractStub.GetSubscriptionCount.CallAsync(new Empty());
            output.Value.ShouldBe(subscriptionCount + 1);
        }
        {
            var output = await OracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = subscriptionCount + 1
            });
            output.Owner.ShouldBe(DefaultAddress);
            output.ProposedOwner.ShouldBeNull();
            output.Consumers.Count.ShouldBe(0);
            output.Balance.ShouldBe(0);
            output.BlockBalance.ShouldBe(0);
        }
        {
            var output = await OracleContractStub.IsPendingRequestExists.CallAsync(new Int64Value
            {
                Value = subscriptionCount + 1
            });
            output.Value.ShouldBeFalse();
        }
        {
            var result = await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionCreated>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(subscriptionCount + 2);
            log.Owner.ShouldBe(DefaultAddress);

            var consumerLog = GetLogEvent<SubscriptionConsumerAdded>(result.TransactionResult);
            consumerLog.SubscriptionId.ShouldBe(subscriptionCount + 2);
            consumerLog.Consumer.ShouldBe(DefaultAddress);
        }
        {
            var output = await OracleContractStub.GetSubscriptionCount.CallAsync(new Empty());
            output.Value.ShouldBe(subscriptionCount + 2);
        }
        {
            var output = await OracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = subscriptionCount + 2
            });
            output.Owner.ShouldBe(DefaultAddress);
            output.ProposedOwner.ShouldBeNull();
            output.Consumers.Count.ShouldBe(1);
            output.Consumers[0].ShouldBe(DefaultAddress);
            output.BlockBalance.ShouldBe(0);
            output.Balance.ShouldBe(0);
        }
        {
            var output = await OracleContractStub.GetConsumer.CallAsync(new GetConsumerInput
            {
                SubscriptionId = subscriptionCount + 2,
                Consumer = DefaultAddress
            });
            output.Allowed.ShouldBeTrue();
            output.InitiatedRequests.ShouldBe(0);
            output.CompletedRequests.ShouldBe(0);
        }
        {
            var output = await OracleContractStub.IsPendingRequestExists.CallAsync(new Int64Value
            {
                Value = subscriptionCount + 2
            });
            output.Value.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task CreateSubscriptionTests_Fail()
    {
        {
            var result = await OracleContractStub.CreateSubscription.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }
        {
            var result = await OracleContractStub.CreateSubscriptionWithConsumer.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }

        await InitializeAsync();

        {
            var result = await OracleContractStub.CreateSubscriptionWithConsumer.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var result = await UserOracleContractStub.CreateSubscription.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result =
                await UserOracleContractStub.CreateSubscriptionWithConsumer.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await PauseAsync();
        {
            var result = await OracleContractStub.CreateSubscription.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
        {
            var result = await OracleContractStub.CreateSubscriptionWithConsumer.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
    }

    [Fact]
    public async Task CancelSubscriptionTests()
    {
        await InitializeAsync();

        // CancelSubscription
        {
            const long subscriptionId = 1;

            await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);

            var result = await OracleContractStub.CancelSubscription.SendAsync(new CancelSubscriptionInput
            {
                SubscriptionId = subscriptionId,
                To = DefaultAddress
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionCanceled>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(subscriptionId);
            log.FundsRecipient.ShouldBe(DefaultAddress);
            log.FundsAmount.ShouldBe(0);
        }
        {
            var output = await OracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = 1
            });
            output.ShouldBe(new Subscription());
        }
        {
            var output = await OracleContractStub.GetConsumer.CallAsync(new GetConsumerInput
            {
                SubscriptionId = 1,
                Consumer = DefaultAddress
            });
            output.ShouldBe(new Consumer());
        }
        // CancelSubscription without To
        {
            const long subscriptionId = 2;

            await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);

            var result = await OracleContractStub.CancelSubscription.SendAsync(new CancelSubscriptionInput
            {
                SubscriptionId = subscriptionId,
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionCanceled>(result.TransactionResult);
            log.FundsRecipient.ShouldBe(DefaultAddress);
        }
        // AdminCancelSubscription
        {
            const long subscriptionId = 3;

            await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);

            await OracleContractStub.TransferAdmin.SendAsync(UserAddress);
            await UserOracleContractStub.AcceptAdmin.SendAsync(new Empty());

            var result = await UserOracleContractStub.AdminCancelSubscription.SendAsync(new Int64Value
            {
                Value = subscriptionId
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionCanceled>(result.TransactionResult);
            log.FundsRecipient.ShouldBe(DefaultAddress);
        }
        {
            var output = await OracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = 3
            });
            output.ShouldBe(new Subscription());
        }
        {
            var output = await OracleContractStub.GetConsumer.CallAsync(new GetConsumerInput
            {
                SubscriptionId = 3,
                Consumer = DefaultAddress
            });
            output.ShouldBe(new Consumer());
        }
    }

    [Fact]
    public async Task CancelSubscriptionTests_Fail()
    {
        await InitializeAsync();
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);

        // CancelSubscription
        {
            var result = await UserOracleContractStub.CancelSubscription.SendWithExceptionAsync(
                new CancelSubscriptionInput
                {
                    SubscriptionId = 1
                });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.CancelSubscription.SendWithExceptionAsync(
                new CancelSubscriptionInput
                {
                    SubscriptionId = -1
                });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription id.");
        }
        {
            var result = await OracleContractStub.CancelSubscription.SendWithExceptionAsync(
                new CancelSubscriptionInput
                {
                    SubscriptionId = 2
                });
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }
        {
            var result = await OracleContractStub.CancelSubscription.SendWithExceptionAsync(
                new CancelSubscriptionInput
                {
                    SubscriptionId = 1,
                    To = new Address()
                });
            result.TransactionResult.Error.ShouldContain("Invalid input to address.");
        }
        // AdminCancelSubscription
        {
            var result = await UserOracleContractStub.AdminCancelSubscription.SendWithExceptionAsync(new Int64Value
            {
                Value = 1
            });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.AdminCancelSubscription.SendWithExceptionAsync(new Int64Value
            {
                Value = 0
            });
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var result = await OracleContractStub.AdminCancelSubscription.SendWithExceptionAsync(new Int64Value
            {
                Value = 2
            });
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }
    }

    [Fact]
    public async Task AddAndRemoveConsumerTests()
    {
        const long subscriptionId = 1;

        await InitializeAsync();
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);

        {
            var output = await OracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = subscriptionId
            });
            output.Consumers.Count.ShouldBe(1);
            output.Consumers.Last().ShouldBe(DefaultAddress);
        }
        // AddConsumer
        {
            var result = await OracleContractStub.AddConsumer.SendAsync(new AddConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = UserAddress
            });

            var log = GetLogEvent<SubscriptionConsumerAdded>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(subscriptionId);
            log.Consumer.ShouldBe(UserAddress);

            var output = await OracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = subscriptionId
            });
            output.Consumers.Count.ShouldBe(2);
            output.Consumers.ShouldBe(new List<Address> { DefaultAddress, UserAddress });
        }
        {
            var result = await OracleContractStub.AddConsumer.SendAsync(new AddConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = UserAddress
            });

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name == "SubscriptionConsumerAdded");
            log.ShouldBeNull();
        }
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(Accounts[2].Address);
        {
            var result = await OracleContractStub.AddConsumer.SendAsync(new AddConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = Accounts[2].Address
            });

            var log = GetLogEvent<SubscriptionConsumerAdded>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(subscriptionId);
            log.Consumer.ShouldBe(Accounts[2].Address);

            var output = await OracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = subscriptionId
            });
            output.Consumers.Count.ShouldBe(3);
            output.Consumers.ShouldBe(new List<Address> { DefaultAddress, UserAddress, Accounts[2].Address });
        }
        // RemoveConsumer
        {
            var result = await OracleContractStub.RemoveConsumer.SendAsync(new RemoveConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = UserAddress
            });

            var log = GetLogEvent<SubscriptionConsumerRemoved>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(subscriptionId);
            log.Consumer.ShouldBe(UserAddress);

            var output = await OracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = subscriptionId
            });
            output.Consumers.Count.ShouldBe(2);
            output.Consumers.ShouldBe(new List<Address> { DefaultAddress, Accounts[2].Address });
        }
        {
            var result = await OracleContractStub.RemoveConsumer.SendAsync(new RemoveConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = UserAddress
            });

            result.TransactionResult.Logs.FirstOrDefault(l => l.Name == "SubscriptionConsumerRemoved")
                .ShouldBeNull();
        }
        await OracleContractStub.CreateSubscription.SendAsync(new Empty());
        {
            var result = await OracleContractStub.RemoveConsumer.SendAsync(new RemoveConsumerInput
            {
                SubscriptionId = 3,
                Consumer = UserAddress
            });

            result.TransactionResult.Logs.FirstOrDefault(l => l.Name == "SubscriptionConsumerRemoved")
                .ShouldBeNull();
        }
    }

    [Fact]
    public async Task AddOrRemoveConsumerTests_Fail()
    {
        const long subscriptionId = 1;

        await InitializeAsync();
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);

        // AddConsumer
        {
            var result = await UserOracleContractStub.AddConsumer.SendWithExceptionAsync(new AddConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = UserAddress
            });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.AddConsumer.SendWithExceptionAsync(new AddConsumerInput
            {
                SubscriptionId = 0
            });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription id.");
        }
        {
            var result = await OracleContractStub.AddConsumer.SendWithExceptionAsync(new AddConsumerInput
            {
                SubscriptionId = subscriptionId
            });
            result.TransactionResult.Error.ShouldContain("Invalid input consumer address.");
        }
        {
            var result = await OracleContractStub.AddConsumer.SendWithExceptionAsync(new AddConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input consumer address.");
        }
        {
            var result = await OracleContractStub.AddConsumer.SendWithExceptionAsync(new AddConsumerInput
            {
                SubscriptionId = subscriptionId + 1,
                Consumer = UserAddress
            });
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }
        await OracleContractStub.SetSubscriptionConfig.SendAsync(new SubscriptionConfig
        {
            MaxConsumersPerSubscription = 1
        });
        {
            var result = await OracleContractStub.AddConsumer.SendWithExceptionAsync(new AddConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = UserAddress
            });
            result.TransactionResult.Error.ShouldContain("Too many consumers.");
        }

        // RemoveConsumer
        {
            var result = await UserOracleContractStub.RemoveConsumer.SendWithExceptionAsync(new RemoveConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.RemoveConsumer.SendWithExceptionAsync(new RemoveConsumerInput
            {
                SubscriptionId = 0
            });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription id.");
        }
        {
            var result = await OracleContractStub.RemoveConsumer.SendWithExceptionAsync(new RemoveConsumerInput
            {
                SubscriptionId = subscriptionId
            });
            result.TransactionResult.Error.ShouldContain("Invalid input consumer address.");
        }
        {
            var result = await OracleContractStub.RemoveConsumer.SendWithExceptionAsync(new RemoveConsumerInput
            {
                SubscriptionId = subscriptionId,
                Consumer = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input consumer address.");
        }
        {
            var result = await OracleContractStub.RemoveConsumer.SendWithExceptionAsync(new RemoveConsumerInput
            {
                SubscriptionId = subscriptionId + 1,
                Consumer = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }
    }

    [Fact]
    public async Task SubscriptionOwnerTransferTests()
    {
        const long subscriptionId = 1;

        await InitializeAsync();
        await OracleContractStub.CreateSubscription.SendAsync(new Empty());

        {
            var result = await OracleContractStub.ProposeSubscriptionOwnerTransfer.SendAsync(
                new ProposeSubscriptionOwnerTransferInput
                {
                    SubscriptionId = subscriptionId,
                    To = UserAddress
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionOwnerTransferRequested>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(subscriptionId);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(UserAddress);

            var output = await OracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = subscriptionId
            });
            output.Owner.ShouldBe(DefaultAddress);
            output.ProposedOwner.ShouldBe(UserAddress);
        }
        {
            var result = await OracleContractStub.ProposeSubscriptionOwnerTransfer.SendAsync(
                new ProposeSubscriptionOwnerTransferInput
                {
                    SubscriptionId = subscriptionId,
                    To = UserAddress
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name == "SubscriptionOwnerTransferRequested");
            log.ShouldBeNull();
        }
        {
            var result = await UserOracleContractStub.AcceptSubscriptionOwnerTransfer.SendAsync(new Int64Value
            {
                Value = subscriptionId
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<SubscriptionOwnerTransferred>(result.TransactionResult);
            log.SubscriptionId.ShouldBe(1);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(UserAddress);

            var output = await UserOracleContractStub.GetSubscription.CallAsync(new Int64Value
            {
                Value = 1
            });
            output.Owner.ShouldBe(UserAddress);
            output.ProposedOwner.ShouldBe(new Address());
        }
    }

    [Fact]
    public async Task SubscriptionOwnerTransferTests_Fail()
    {
        const long subscriptionId = 1;

        await InitializeAsync();
        await OracleContractStub.CreateSubscription.SendAsync(new Empty());

        {
            var result = await UserOracleContractStub.ProposeSubscriptionOwnerTransfer.SendWithExceptionAsync(
                new ProposeSubscriptionOwnerTransferInput
                {
                    SubscriptionId = subscriptionId,
                    To = UserAddress
                });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.ProposeSubscriptionOwnerTransfer.SendWithExceptionAsync(
                new ProposeSubscriptionOwnerTransferInput
                {
                    SubscriptionId = 0
                });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription id.");
        }
        {
            var result = await OracleContractStub.ProposeSubscriptionOwnerTransfer.SendWithExceptionAsync(
                new ProposeSubscriptionOwnerTransferInput
                {
                    SubscriptionId = subscriptionId + 1,
                    To = UserAddress
                });
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }
        {
            var result = await OracleContractStub.ProposeSubscriptionOwnerTransfer.SendWithExceptionAsync(
                new ProposeSubscriptionOwnerTransferInput
                {
                    SubscriptionId = subscriptionId
                });
            result.TransactionResult.Error.ShouldContain("Invalid input to address.");
        }
        {
            var result = await OracleContractStub.ProposeSubscriptionOwnerTransfer.SendWithExceptionAsync(
                new ProposeSubscriptionOwnerTransferInput
                {
                    SubscriptionId = subscriptionId,
                    To = new Address()
                });
            result.TransactionResult.Error.ShouldContain("Invalid input to address.");
        }
        {
            var result = await OracleContractStub.ProposeSubscriptionOwnerTransfer.SendWithExceptionAsync(
                new ProposeSubscriptionOwnerTransferInput
                {
                    SubscriptionId = subscriptionId,
                    To = DefaultAddress
                });
            result.TransactionResult.Error.ShouldContain("Cannot transfer to self.");
        }
        {
            var result = await UserOracleContractStub.AcceptSubscriptionOwnerTransfer.SendWithExceptionAsync(
                new Int64Value
                {
                    Value = subscriptionId
                });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        await OracleContractStub.ProposeSubscriptionOwnerTransfer.SendAsync(new ProposeSubscriptionOwnerTransferInput
        {
            SubscriptionId = subscriptionId,
            To = UserAddress
        });
        {
            var result = await OracleContractStub.AcceptSubscriptionOwnerTransfer.SendWithExceptionAsync(new Int64Value
            {
                Value = subscriptionId
            });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await UserOracleContractStub.AcceptSubscriptionOwnerTransfer.SendWithExceptionAsync(
                new Int64Value
                {
                    Value = 0
                });
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
        {
            var result = await UserOracleContractStub.AcceptSubscriptionOwnerTransfer.SendWithExceptionAsync(
                new Int64Value
                {
                    Value = subscriptionId + 1
                });
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }
    }
}