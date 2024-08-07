using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.AIFeeds;

public partial class AIFeedsContract : AIFeedsContractContainer.AIFeedsContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");
        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractAuthor.Call(Context.Self) == Context.Sender, "No permission.");
        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");
        State.Admin.Value = input.Admin ?? Context.Sender;
        State.Initialized.Value = true;

        return new Empty();
    }

    public override Address GetAdmin(Empty input) => State.Admin.Value;

    public override Empty SetConfig(AIOracleConfig input)
    {
        Assert(IsAdminValid(), "No set config permission.");
        Assert(input != null, "Invalid input.");
        Assert(input.AiRequestFees >= 0, "Invalid ai request fee.");
        Assert(input.Enclaves != null, "Invalid input enclaves.");

        State.Config.Value = input;
        Context.Fire(new AIOracleConfigSet { Config = State.Config.Value });

        return new Empty();
    }

    public override AIOracleConfig GetConfig(Empty input) => State.Config.Value;
}