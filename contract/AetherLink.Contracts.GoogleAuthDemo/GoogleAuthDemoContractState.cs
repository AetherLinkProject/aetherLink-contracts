using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.GoogleAuthDemo;

public partial class GoogleAuthDemoContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }
    public MappedState<Hash, GoogleRoundData> FulfillData { get; set; }
    public SingletonState<long> LatestRound { get; set; }
}