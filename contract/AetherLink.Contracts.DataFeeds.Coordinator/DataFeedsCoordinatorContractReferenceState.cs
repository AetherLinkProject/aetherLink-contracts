using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using AetherLink.Contracts.Oracle;

namespace AetherLink.Contracts.DataFeeds.Coordinator;

public partial class DataFeedsCoordinatorContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
}