using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.DataFeeds.Coordinator;

public partial class DataFeedsCoordinatorContract
{
    public override Address GetAdmin(Empty input)
    {
        return State.Admin.Value;
    }

    public override BoolValue IsPaused(Empty input)
    {
        return new BoolValue
        {
            Value = State.Paused.Value
        };
    }

    public override Address GetOracleContractAddress(Empty input)
    {
        return State.OracleContract.Value;
    }

    public override Config GetConfig(Empty input)
    {
        return State.Config.Value;
    }

    public override Int32Value GetRequestTypeIndex(Empty input)
    {
        return new Int32Value
        {
            Value = State.RequestTypeIndex.Value
        };
    }

    public override Hash GetCommitmentHash(Hash input)
    {
        return State.RequestCommitmentMap[input];
    }
}