using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContract : OracleContractContainer.OracleContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");

        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");

        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");

        State.Admin.Value = input.Admin ?? Context.Sender;
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.MaxOracleCount.Value = OracleContractConstants.DefaultMaxOracleAmount;
        State.SubscriptionConfig.Value = new SubscriptionConfig
        {
            MaxConsumersPerSubscription = OracleContractConstants.DefaultMaxConsumersPerSubscription
        };
        State.Initialized.Value = true;

        return new Empty();
    }

    public override Empty TransferAdmin(Address input)
    {
        CheckAdminPermission();
        Assert(IsAddressValid(input), "Invalid input admin.");
        Assert(input != State.Admin.Value, "Cannot transfer to self.");

        State.PendingAdmin.Value = input;

        Context.Fire(new AdminTransferRequested
        {
            From = Context.Sender,
            To = input
        });

        return new Empty();
    }

    public override Empty AcceptAdmin(Empty input)
    {
        Assert(Context.Sender == State.PendingAdmin.Value, "No permission.");

        var from = State.Admin.Value.Clone();

        State.Admin.Value = Context.Sender;
        State.PendingAdmin.Value = new Address();

        Context.Fire(new AdminTransferred
        {
            From = from,
            To = Context.Sender
        });

        return new Empty();
    }

    public override Empty Pause(Empty input)
    {
        CheckAdminPermission();
        Assert(!State.Paused.Value, "Already paused.");

        State.Paused.Value = true;

        Context.Fire(new Paused
        {
            Account = Context.Sender
        });

        return new Empty();
    }

    public override Empty Unpause(Empty input)
    {
        CheckAdminPermission();
        Assert(State.Paused.Value, "Contract not on pause.");

        State.Paused.Value = false;

        Context.Fire(new Unpaused
        {
            Account = Context.Sender
        });

        return new Empty();
    }

    public override Empty AddCoordinator(Address input)
    {
        CheckAdminPermission();
        Assert(IsAddressValid(input), "Invalid input.");

        var index = State.CurrentRequestTypeIndex.Value.Add(1);

        State.Coordinators[index] = new Coordinator
        {
            RequestTypeIndex = index,
            Status = true,
            CoordinatorContractAddress = input
        };

        State.CurrentRequestTypeIndex.Value = index;

        Context.Fire(new CoordinatorSet
        {
            CoordinatorContractAddress = input,
            RequestTypeIndex = index,
            Status = true
        });

        return new Empty();
    }

    public override Empty SetCoordinatorStatus(SetCoordinatorStatusInput input)
    {
        CheckAdminPermission();

        Assert(input != null, "Invalid input.");
        Assert(input.RequestTypeIndex > 0 && input.RequestTypeIndex <= State.CurrentRequestTypeIndex.Value,
            "Invalid input coordinator type index.");

        var coordinator = State.Coordinators[input.RequestTypeIndex];
        Assert(coordinator != null, "Coordinator not found.");

        if (coordinator.Status == input.Status)
        {
            return new Empty();
        }

        coordinator.Status = input.Status;

        Context.Fire(new CoordinatorSet
        {
            CoordinatorContractAddress = coordinator.CoordinatorContractAddress,
            RequestTypeIndex = input.RequestTypeIndex,
            Status = input.Status
        });

        return new Empty();
    }

    public override Empty SetSubscriptionConfig(SubscriptionConfig input)
    {
        CheckAdminPermission();

        Assert(input != null, "Invalid input.");
        Assert(input.MaxConsumersPerSubscription > 0, "Invalid input max consumers per subscription.");

        if (State.SubscriptionConfig.Value.Equals(input))
        {
            return new Empty();
        }

        State.SubscriptionConfig.Value = input;

        Context.Fire(new SubscriptionConfigSet
        {
            Config = State.SubscriptionConfig.Value
        });

        return new Empty();
    }
}