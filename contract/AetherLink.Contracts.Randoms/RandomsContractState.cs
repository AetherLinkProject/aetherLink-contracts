using AElf.Contracts.Randoms;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.Randoms;

public partial class RandomsContractState : ContractState
{
    public BoolState Initialized { get; set; }
    public MappedState<Hash, RandomRequestInfo> RandomResults { get; set; }
}