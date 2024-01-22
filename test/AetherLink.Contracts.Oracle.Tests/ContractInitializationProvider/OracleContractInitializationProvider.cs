using System.Collections.Generic;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AetherLink.Contracts.Oracle.ContractInitializationProvider
{
    public class OracleContractInitializationProvider : IContractInitializationProvider
    {
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }

        public Hash SystemSmartContractName { get; } = OracleContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "OracleContract";
    }
}