using System.Collections.Generic;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AetherLink.Contracts.VRF.Coordinator.ContractInitializationProvider;

public class VrfCoordinatorContractInitializationProvider : IContractInitializationProvider
{
    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>();
    }

    public Hash SystemSmartContractName { get; } = VrfCoordinatorContractAddressNameProvider.Name;
    public string ContractCodeName { get; } = "VrfCoordinatorContract";
}