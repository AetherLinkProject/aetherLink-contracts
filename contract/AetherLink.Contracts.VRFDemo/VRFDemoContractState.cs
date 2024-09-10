using AElf.Contracts.VRFDemo;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.VRFDemo;

public partial class VRFDemoContractState : ContractState
{
    public BoolState Initialized { get; set; }
    public MappedState<Hash, RecordInfo> PlayedRecords { get; set; }
}