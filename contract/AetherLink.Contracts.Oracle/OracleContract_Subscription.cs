using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContract
{
    public override Empty CreateSubscription(Empty input)
    {
        CheckUnpause();
        CheckInitialized();

        // TODO Temporary
        CheckAdminPermission();

        var subscriptionId = State.CurrentSubscriptionId.Value.Add(1);
        State.CurrentSubscriptionId.Value = subscriptionId;
        State.Subscriptions[subscriptionId] = new Subscription
        {
            Owner = Context.Sender
        };

        Context.Fire(new SubscriptionCreated
        {
            SubscriptionId = subscriptionId,
            Owner = Context.Sender
        });

        return new Empty();
    }

    public override Empty CreateSubscriptionWithConsumer(Address input)
    {
        CheckUnpause();
        CheckInitialized();

        // TODO Temporary
        CheckAdminPermission();

        Assert(IsAddressValid(input), "Invalid input.");

        var subscriptionId = State.CurrentSubscriptionId.Value.Add(1);
        State.CurrentSubscriptionId.Value = subscriptionId;
        State.Subscriptions[subscriptionId] = new Subscription
        {
            Owner = Context.Sender,
            Consumers = { input }
        };

        State.Consumers[input][subscriptionId] = new Consumer
        {
            Allowed = true
        };

        Context.Fire(new SubscriptionCreated
        {
            SubscriptionId = subscriptionId,
            Owner = Context.Sender
        });
        Context.Fire(new SubscriptionConsumerAdded
        {
            SubscriptionId = subscriptionId,
            Consumer = input
        });

        return new Empty();
    }

    public override Empty CancelSubscription(CancelSubscriptionInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.SubscriptionId > 0, "Invalid input subscription id.");
        Assert(input.To == null || IsAddressValid(input.To), "Invalid input to address.");

        var subscription = State.Subscriptions[input.SubscriptionId]?.Clone();

        Assert(subscription != null, "Subscription not found.");
        Assert(subscription.Owner == Context.Sender, "No permission.");

        foreach (var address in subscription.Consumers)
        {
            var consumer = State.Consumers[address][input.SubscriptionId];

            Assert(consumer.CompletedRequests >= consumer.InitiatedRequests,
                "Cannot cancel subscription with pending requests.");

            State.Consumers[address].Remove(input.SubscriptionId);
        }

        State.Subscriptions.Remove(input.SubscriptionId);

        Context.Fire(new SubscriptionCanceled
        {
            SubscriptionId = input.SubscriptionId,
            FundsRecipient = input.To ?? subscription.Owner,
            FundsAmount = subscription.Balance
        });

        return new Empty();
    }

    public override Empty AdminCancelSubscription(Int64Value input)
    {
        CheckAdminPermission();

        Assert(input != null && input.Value > 0, "Invalid input.");

        var subscription = State.Subscriptions[input.Value]?.Clone();

        Assert(subscription != null, "Subscription not found.");

        foreach (var consumer in subscription.Consumers)
        {
            State.Consumers[consumer].Remove(input.Value);
        }

        State.Subscriptions.Remove(input.Value);

        Context.Fire(new SubscriptionCanceled
        {
            SubscriptionId = input.Value,
            FundsRecipient = subscription.Owner,
            FundsAmount = subscription.Balance
        });

        return new Empty();
    }

    public override Empty ProposeSubscriptionOwnerTransfer(ProposeSubscriptionOwnerTransferInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.SubscriptionId > 0, "Invalid input subscription id.");
        Assert(IsAddressValid(input.To), "Invalid input to address.");

        var subscription = State.Subscriptions[input.SubscriptionId];

        Assert(subscription != null, "Subscription not found.");
        Assert(subscription.Owner == Context.Sender, "No permission.");
        Assert(subscription.Owner != input.To, "Cannot transfer to self.");

        if (subscription.ProposedOwner == input.To)
        {
            return new Empty();
        }

        subscription.ProposedOwner = input.To;

        Context.Fire(new SubscriptionOwnerTransferRequested
        {
            SubscriptionId = input.SubscriptionId,
            From = subscription.Owner,
            To = input.To
        });

        return new Empty();
    }

    public override Empty AcceptSubscriptionOwnerTransfer(Int64Value input)
    {
        Assert(input != null && input.Value > 0, "Invalid input.");

        var subscription = State.Subscriptions[input.Value];

        Assert(subscription != null, "Subscription not found.");
        Assert(subscription.ProposedOwner == Context.Sender, "No permission.");

        var from = subscription.Owner.Clone();
        subscription.Owner = subscription.ProposedOwner.Clone();
        subscription.ProposedOwner = new Address();

        Context.Fire(new SubscriptionOwnerTransferred
        {
            SubscriptionId = input.Value,
            From = from,
            To = subscription.Owner
        });

        return new Empty();
    }

    public override Empty AddConsumer(AddConsumerInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.SubscriptionId > 0, "Invalid input subscription id.");
        Assert(IsAddressValid(input.Consumer), "Invalid input consumer address.");

        var subscription = State.Subscriptions[input.SubscriptionId];

        Assert(subscription != null, "Subscription not found.");
        Assert(subscription.Owner == Context.Sender, "No permission.");

        var consumer = State.Consumers[input.Consumer][input.SubscriptionId];
        if (consumer != null && consumer.Allowed)
        {
            return new Empty();
        }

        Assert(subscription.Consumers.Count < State.SubscriptionConfig.Value.MaxConsumersPerSubscription,
            "Too many consumers.");

        subscription.Consumers.Add(input.Consumer);
        State.Consumers[input.Consumer][input.SubscriptionId] = new Consumer
        {
            Allowed = true
        };

        Context.Fire(new SubscriptionConsumerAdded
        {
            SubscriptionId = input.SubscriptionId,
            Consumer = input.Consumer
        });

        return new Empty();
    }

    public override Empty RemoveConsumer(RemoveConsumerInput input)
    {
        Assert(input != null, "Invalid input.");
        Assert(input.SubscriptionId > 0, "Invalid input subscription id.");
        Assert(IsAddressValid(input.Consumer), "Invalid input consumer address.");

        var subscription = State.Subscriptions[input.SubscriptionId];

        Assert(subscription != null, "Subscription not found.");
        Assert(subscription.Owner == Context.Sender, "No permission.");

        if (subscription.Consumers.Count == 0)
        {
            return new Empty();
        }

        var consumer = State.Consumers[input.Consumer][input.SubscriptionId];

        if (consumer == null || !consumer.Allowed)
        {
            return new Empty();
        }

        Assert(consumer.CompletedRequests >= consumer.InitiatedRequests,
            "Cannot remove consumer with pending requests.");

        State.Consumers[input.Consumer].Remove(input.SubscriptionId);
        subscription.Consumers.Remove(input.Consumer);

        Context.Fire(new SubscriptionConsumerRemoved
        {
            SubscriptionId = input.SubscriptionId,
            Consumer = input.Consumer
        });

        return new Empty();
    }
}