using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Ramp;

public partial class RampContract : RampContractContainer.RampContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");

        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender,
            "No initialize permission.");

        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");
        Assert(input.Oracle == null || !input.Oracle.Value.IsNullOrEmpty(), "Invalid input oracle contract.");

        State.Admin.Value = input.Admin ?? Context.Sender;
        State.OracleContract.Value = input.Oracle;
        State.Initialized.Value = true;

        return new Empty();
    }

    public override Empty SetConfig(Config input)
    {
        CheckAdminPermission();

        Assert(input != null, "Invalid SetConfig input.");
        Assert(input.ChainIdList?.Data?.Count > 0, "Invalid input chain id list.");

        if (State.Config.Value != null && State.Config.Value.Equals(input)) return new Empty();

        State.Config.Value = input;
        State.LatestEpoch.Value = 0;

        Context.Fire(new ConfigSet { Config = State.Config.Value });

        return new Empty();
    }

    public override Empty SetOracleContractAddress(Address input)
    {
        CheckAdminPermission();

        Assert(input != null && IsAddressValid(input), "Invalid Oracle Address input.");

        State.OracleContract.Value = input;

        return new Empty();
    }

    public override Empty AddRampSender(AddRampSenderInput input)
    {
        CheckAdminPermission();

        Assert(input != null, "Invalid AddRampSender input.");
        var sender = input.SenderAddress;
        Assert(sender != null, "Invalid ramp sender address.");
        Assert(State.RampSenders[sender] == null, "Sender was existed.");

        State.RampSenders[input.SenderAddress] = new() { SenderAddress = sender, Created = Context.CurrentBlockTime };
        Context.Fire(new RampSenderAdded { SenderAddress = sender });

        return new Empty();
    }

    public override Empty RemoveRampSender(Address senderAddress)
    {
        CheckAdminPermission();

        Assert(IsAddressValid(senderAddress), "Invalid sender address to remove.");
        Assert(State.RampSenders[senderAddress] != null, "Sender is not existed.");

        State.RampSenders.Remove(senderAddress);
        Context.Fire(new RampSenderRemoved { SenderAddress = senderAddress });

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

    public override Empty SetTokenSwapConfig(TokenSwapConfig input)
    {
        Assert(State.RampSenders[Context.Sender] != null,
            "The sender does not have permission to set TokenSwap config.");
        State.TokenSwapConfigMap[Context.Sender] = input;

        Context.Fire(new TokenSwapConfigUpdated
        {
            ContractAddress = Context.Sender,
            TokenSwapList = input.TokenSwapList
        });
        return new Empty();
    }

    public override TokenSwapConfig GetTokenSwapConfig(Address sender) => State.TokenSwapConfigMap[sender];
}