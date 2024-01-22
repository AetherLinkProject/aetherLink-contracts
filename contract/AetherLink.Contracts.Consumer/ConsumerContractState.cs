using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.Consumer;

public partial class ConsumerContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    public SingletonState<Address> PendingAdmin { get; set; }
    public SingletonState<AddressList> Controller { get; set; }
    public MappedState<Hash, OracleResponse> OracleResponses { get; set; }
    public MappedState<Hash, LongList> Prices { get; set; }
    public MappedState<Hash, HashList> RandomHashes { get; set; }
    public SingletonState<PriceRoundData> LatestPriceRoundData { get; set; }
    public SingletonState<long> LatestRound { get; set; }
    public MappedState<long, PriceRoundData> PriceRoundData { get; set; }
    public SingletonState<int> DataFeedsRequestTypeIndex { get; set; }
    public SingletonState<int> VrfRequestTypeIndex { get; set; }
}