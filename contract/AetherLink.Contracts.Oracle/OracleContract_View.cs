using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContract
{
    public override Address GetAdmin(Empty input)
    {
        return State.Admin.Value;
    }

    public override GetConfigOutput GetConfig(Empty input)
    {
        return new GetConfigOutput
        {
            Config = State.Config.Value,
            Signers = { State.Signers.Value.Data },
            Transmitters = { State.Transmitters.Value.Data }
        };
    }

    public override Int64Value GetMaxOracleCount(Empty input)
    {
        return new Int64Value
        {
            Value = State.MaxOracleCount.Value
        };
    }

    public override GetLatestConfigDetailsOutput GetLatestConfigDetails(Empty input)
    {
        return new GetLatestConfigDetailsOutput
        {
            ConfigCount = State.ConfigCount.Value,
            BlockNumber = State.LatestConfigBlockNumber.Value,
            ConfigDigest = State.Config.Value.LatestConfigDigest
        };
    }

    public override Int64Value GetLatestRound(Empty input)
    {
        return new Int64Value
        {
            Value = State.LatestRound.Value
        };
    }

    public override Oracle GetOracle(Address input)
    {
        return State.OraclesMap[input];
    }

    public override AddressList GetTransmitters(Empty input)
    {
        return State.Transmitters.Value;
    }

    public override HashList GetProvingKeyHashes(Empty input)
    {
        return State.ProvingKeyHashes.Value;
    }

    public override Address GetOracleByProvingKeyHash(StringValue input)
    {
        return State.ProvingKeyOraclesMap[ComputeHashOfKey(input.Value)];
    }

    public override Hash GetHashFromKey(StringValue input)
    {
        return ComputeHashOfKey(input.Value);
    }

    public override CoordinatorList GetCoordinators(Empty input)
    {
        var list = new List<Coordinator>();

        for (var i = 0; i <= State.CurrentRequestTypeIndex.Value; i++)
        {
            var coordinator = State.Coordinators[i];
            if (coordinator != null && coordinator.Status)
            {
                list.Add(State.Coordinators[i]);
            }
        }

        return new CoordinatorList
        {
            Data = { list }
        };
    }

    public override Coordinator GetCoordinatorByIndex(Int32Value input)
    {
        return State.Coordinators[input.Value];
    }

    public override SubscriptionConfig GetSubscriptionConfig(Empty input)
    {
        return State.SubscriptionConfig.Value;
    }

    public override BoolValue IsPaused(Empty input)
    {
        return new BoolValue
        {
            Value = State.Paused.Value
        };
    }

    public override BoolValue IsPendingRequestExists(Int64Value input)
    {
        var consumers = State.Subscriptions[input.Value]?.Consumers;
        if (consumers == null || consumers.Count == 0)
        {
            return new BoolValue { Value = false };
        }

        foreach (var address in consumers)
        {
            var consumer = State.Consumers[address][input.Value];
            if (consumer.CompletedRequests < consumer.InitiatedRequests)
            {
                return new BoolValue { Value = true };
            }
        }

        return new BoolValue { Value = false };
    }

    public override Subscription GetSubscription(Int64Value input)
    {
        return State.Subscriptions[input.Value];
    }

    public override Consumer GetConsumer(GetConsumerInput input)
    {
        return State.Consumers[input.Consumer][input.SubscriptionId];
    }

    public override Int64Value GetSubscriptionCount(Empty input)
    {
        return new Int64Value
        {
            Value = State.CurrentSubscriptionId.Value
        };
    }
}