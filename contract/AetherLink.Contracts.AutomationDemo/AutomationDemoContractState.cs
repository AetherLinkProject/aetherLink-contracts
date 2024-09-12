using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AetherLink.Contracts.AutomationDemo;

public partial class AutomationDemoContractState : ContractState
{
    public SingletonState<bool> Initialized { get; set; }
    public SingletonState<Address> Admin { get; set; }

    public MappedState<Hash, OrderRecord> OrderRecordMap { get; set; }
    public MappedState<Hash, InvestmentInfo> InvestmentInfoMap { get; set; }
}