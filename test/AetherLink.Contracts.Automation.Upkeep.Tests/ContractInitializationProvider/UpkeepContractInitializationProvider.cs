using System.Collections.Generic;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AetherLink.Contracts.Automation.Upkeep.ContractInitializationProvider;

public class UpkeepContractInitializationProvider : IContractInitializationProvider
{
    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>();
    }

    public Hash SystemSmartContractName { get; } = UpkeepContractAddressNameProvider.Name;
    public string ContractCodeName { get; } = "UpkeepContract";
}