using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.Ramp;

public partial class RampContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    public SingletonState<Address> PendingAdmin { get; set; }
    public SingletonState<Config> Config { get; set; }
    public MappedState<Address, RampSenderInfo> RampSenders { get; set; }
    public MappedState<Hash, MessageInfo> MessageInfoMap { get; set; }
    public MappedState<Hash, Hash> ReceivedMessageInfoMap { get; set; }
    public MappedState<Address, TokenSwapConfig> TokenSwapConfigMap { get; set; }
    public SingletonState<long> LatestEpoch { get; set; }
}