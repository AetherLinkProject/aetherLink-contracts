using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.AIFeeds.Demo;

public partial class AIFeedsDemoContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    public MappedState<Hash, AIRequestInfo> AIRequestInfoMap { get; set; }
    internal AIFeedsContractContainer.AIFeedsContractReferenceState AIFeedsContract { get; set; }
}