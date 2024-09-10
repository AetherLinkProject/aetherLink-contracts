using AElf.Contracts.MultiToken;
using AetherLink.Contracts.Oracle;

namespace AetherLink.Contracts.DataFeedsDemo;

public partial class DataFeedsDemoContractState
{
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
}