using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;
using AetherLink.Contracts.Automation;

namespace AetherLink.Contracts.AutomationDemo;

public partial class AutomationDemoContractState
{
    internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    internal AutomationContractContainer.AutomationContractReferenceState AutomationContract { get; set; }
}