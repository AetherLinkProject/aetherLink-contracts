using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
}