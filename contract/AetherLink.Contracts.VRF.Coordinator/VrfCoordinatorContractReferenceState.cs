using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using AetherLink.Contracts.Oracle;

namespace AetherLink.Contracts.VRF.Coordinator;

public partial class VrfCoordinatorContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract { get; set; }
    internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
}