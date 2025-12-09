using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.VRF.Coordinator;

public partial class VrfCoordinatorContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    public SingletonState<Config> Config { get; set; }
    public SingletonState<Address> PendingAdmin { get; set; }
    public SingletonState<bool> Paused { get; set; }
    public MappedState<Hash, Hash> RequestCommitmentMap { get; set; }
    public SingletonState<int> RequestTypeIndex { get; set; }
}