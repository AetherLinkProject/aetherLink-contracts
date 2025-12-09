using AElf.Standards.ACS0;
using AetherLink.Contracts.Oracle;

namespace AetherLink.Contracts.Ramp;

public partial class RampContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
}