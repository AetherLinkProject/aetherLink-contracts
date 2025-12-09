using System.Collections.Generic;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AetherLink.Contracts.Ramp.ContractInitializationProvider;

public class RampContractInitializationProvider : IContractInitializationProvider
{
    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>();
    }

    public Hash SystemSmartContractName { get; } = RampContractAddressNameProvider.Name;
    public string ContractCodeName { get; } = "RampContract";
}