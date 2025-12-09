using System.Collections.Generic;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AetherLink.Contracts.Consumer.ContractInitializationProvider;

public class ConsumerContractInitializationProvider : IContractInitializationProvider
{
    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>();
    }

    public Hash SystemSmartContractName { get; } = ConsumerContractAddressNameProvider.Name;
    public string ContractCodeName { get; } = "ConsumerContract";
}