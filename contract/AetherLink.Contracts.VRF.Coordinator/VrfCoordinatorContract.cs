using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Coordinator;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.VRF.Coordinator;

public partial class VrfCoordinatorContract : VrfCoordinatorContractContainer.VrfCoordinatorContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");

        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");

        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");
        Assert(input.Oracle == null || !input.Oracle.Value.IsNullOrEmpty(), "Invalid input oracle contract.");

        State.Admin.Value = input.Admin ?? Context.Sender;
        State.OracleContract.Value = input.Oracle;
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.ConsensusContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

        State.Config.Value = new Config
        {
            RequestTimeoutSeconds = VrfCoordinatorContractConstants.DefaultRequestTimeoutSeconds,
            MinimumRequestConfirmations = VrfCoordinatorContractConstants.DefaultMinimumRequestConfirmations,
            MaxRequestConfirmations = VrfCoordinatorContractConstants.DefaultMaxRequestConfirmations,
            MaxNumWords = VrfCoordinatorContractConstants.DefaultMaxNumWords
        };
        State.RequestTypeIndex.Value = input.RequestTypeIndex;
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

    public override Empty SetConfig(Config input)
    {
        CheckAdminPermission();

        Assert(input != null, "Invalid input.");
        Assert(input.MaxNumWords > 0, "Invalid input max num words.");
        Assert(input.MinimumRequestConfirmations >= 0, "Invalid input minimum request confirmations.");
        Assert(input.MaxRequestConfirmations >= input.MinimumRequestConfirmations,
            "Invalid input max request confirmations.");
        Assert(input.RequestTimeoutSeconds >= 0, "Invalid input request timeout seconds.");

        if (State.Config.Value.Equals(input))
        {
            return new Empty();
        }

        State.Config.Value = input;

        Context.Fire(new ConfigSet
        {
            Config = State.Config.Value
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

    public override Empty SetOracleContractAddress(Address input)
    {
        CheckAdminPermission();

        Assert(input != null && IsAddressValid(input), "Invalid input.");

        State.OracleContract.Value = input;

        return new Empty();
    }

    public override Empty SetRequestTypeIndex(Int32Value input)
    {
        CheckAdminPermission();

        Assert(input != null && input.Value > 0, "Invalid input.");

        if (State.RequestTypeIndex.Value == input.Value)
        {
            return new Empty();
        }

        State.RequestTypeIndex.Value = input.Value;

        Context.Fire(new RequestTypeIndexSet
        {
            RequestTypeIndex = input.Value
        });
        return new Empty();
    }
}