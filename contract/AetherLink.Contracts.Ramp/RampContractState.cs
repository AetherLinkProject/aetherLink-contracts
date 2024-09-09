using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.Ramp;

public partial class RampContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    public SingletonState<Config> Config { get; set; }
    public MappedState<Address, RampSenderInfo> RampSenders { get; set; }
    public MappedState<Hash, MessageInfo> MessageInfoMap { get; set; }
}