using AetherLink.Contracts.Oracle;

namespace AetherLink.Contracts.Randoms;

public partial class RandomsContractState
{
    internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
}