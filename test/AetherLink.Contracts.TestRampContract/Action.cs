using Google.Protobuf.WellKnownTypes;
using Ramp;

namespace AetherLink.Contracts.TestRampContract;

public class TestRampContract : TestRampContractContainer.TestRampContractBase
{
    public override Empty ForwardMessage(ForwardMessageInput input)
    {
        return new Empty();
    }
}