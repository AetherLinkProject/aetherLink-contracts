using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS0;
using AElf.Types;

namespace AetherLink.Contracts.AIFeeds;

public partial class AIFeedsContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }

    public SingletonState<AIOracleConfig> Config { get; set; }
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }

    public MappedState<Hash, AIRequest> AIRequestInfoMap { get; set; }
}