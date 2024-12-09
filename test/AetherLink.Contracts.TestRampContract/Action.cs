using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.TestRampContract;

public class TestRampContract : TestRampContractContainer.TestRampContractBase
{
    public override Empty ForwardMessage(ForwardMessageInput input)
    {
        return new Empty();
    }

    public override RateLimiterTokenBucket GetCurrentTokenSwapBucketState(GetCurrentTokenSwapBucketStateInput input)
    {
        return new RateLimiterTokenBucket();
    }
}