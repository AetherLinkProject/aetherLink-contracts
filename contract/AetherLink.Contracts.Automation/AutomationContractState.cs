using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS0;
using AElf.Types;
using AetherLink.Contracts.Oracle;

namespace AetherLink.Contracts.Automation;

public partial class AutomationContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    public SingletonState<Address> PendingAdmin { get; set; }
    public SingletonState<bool> Paused { get; set; }
    public SingletonState<Config> Config { get; set; }
    public SingletonState<int> RequestTypeIndex { get; set; }
    public SingletonState<long> SubscriptionId { get; set; }
    public MappedState<Hash, UpkeepInfo> RegisteredUpkeepMap { get; set; }
}