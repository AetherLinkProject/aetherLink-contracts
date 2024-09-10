using AElf.Contracts.DataFeedsDemo;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.DataFeedsDemo;

public partial class DataFeedsDemoContractState : ContractState
{
    // A state to check if contract is initialized
    public BoolState Initialized { get; set; }
    public MappedState<Hash, PriceData> PriceDataMap { get; set; }
    public SingletonState<Price> NftPrice { get; set; }
}