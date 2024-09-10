using AElf.Contracts.MultiToken;
using AetherLink.Contracts.Oracle;

namespace AetherLink.Contracts.VRFDemo;

public partial class VRFDemoContractState
{
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
}