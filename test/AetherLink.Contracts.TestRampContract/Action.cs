using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.TestRampContract;

public class TestRampContract : TestRampContractContainer.TestRampContractBase
{
    public override Empty ForwardMessage(ForwardMessageInput input)
    {
        var addr = Address.Parser.ParseFrom(input.Receiver);
        return new Empty();
    }

    public override RateLimiterTokenBucket GetCurrentTokenSwapBucketState(GetCurrentTokenSwapBucketStateInput input)
    {
        return new RateLimiterTokenBucket();
    }
}