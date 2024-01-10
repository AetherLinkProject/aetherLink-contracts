using System.Collections.Generic;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AetherLink.Contracts.DataFeeds.Coordinator.ContractInitializationProvider;

public class DataFeedsCoordinatorContractInitializationProvider : IContractInitializationProvider
{
    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>();
    }

    public Hash SystemSmartContractName { get; } = DataFeedsCoordinatorContractAddressNameProvider.Name;
    public string ContractCodeName { get; } = "DataFeedsCoordinatorContract";
}