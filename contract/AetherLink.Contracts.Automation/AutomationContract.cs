using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Automation;

public partial class AutomationContract : AutomationContractContainer.AutomationContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");

        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractAuthor.Call(Context.Self) == Context.Sender, "No permission.");
        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");
        State.Admin.Value = input.Admin ?? Context.Sender;

        Assert(input.Oracle == null || !input.Oracle.Value.IsNullOrEmpty(), "Invalid input oracle.");
        if (input.Oracle != null) State.OracleContract.Value = input.Oracle;

        State.Config.Value = new Config
            { RequestTimeoutSeconds = AutomationContractConstants.DefaultRequestTimeoutSeconds };

        State.RequestTypeIndex.Value = input.AutomationTypeIndex;
        State.SubscriptionId.Value = input.SubscriptionId;
        State.Initialized.Value = true;

        return new Empty();
    }

    public override Empty Pause(Empty input)
    {
        CheckAdminPermission();
        Assert(!State.Paused.Value, "Already paused.");

        State.Paused.Value = true;

        Context.Fire(new Paused { Account = Context.Sender });

        return new Empty();
    }

    public override Empty Unpause(Empty input)
    {
        CheckAdminPermission();
        Assert(State.Paused.Value, "Contract not on pause.");

        State.Paused.Value = false;

        Context.Fire(new Unpaused { Account = Context.Sender });

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

        State.RequestTypeIndex.Value = input.Value;

        Context.Fire(new RequestTypeIndexSet { RequestTypeIndex = input.Value });

        return new Empty();
    }

    public override Empty SetSubscriptionId(Int32Value input)
    {
        CheckAdminPermission();
        Assert(input != null && input.Value > 0, "Invalid input.");

        State.SubscriptionId.Value = input.Value;

        Context.Fire(new SubscriptionIdSet { SubscriptionId = input.Value });

        return new Empty();
    }

    public override Address GetAdmin(Empty input) => State.Admin.Value;
    public override BoolValue IsPaused(Empty input) => new() { Value = State.Paused.Value };
    public override Address GetOracleContractAddress(Empty input) => State.OracleContract.Value;
    public override Int32Value GetRequestTypeIndex(Empty input) => new() { Value = State.RequestTypeIndex.Value };
}