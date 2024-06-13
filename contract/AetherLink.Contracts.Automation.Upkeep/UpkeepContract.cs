using AetherLink.Contracts.Upkeep;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Automation.Upkeep;

public class UpkeepContract : UpkeepContractContainer.UpkeepContractBase
{
    public override Empty PerformUpkeep(PerformUpkeepInput input)
    {
        return new Empty();
    }
}