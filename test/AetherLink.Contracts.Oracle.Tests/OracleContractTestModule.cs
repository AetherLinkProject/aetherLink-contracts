using AElf.Boilerplate.TestBase;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AetherLink.Contracts.Oracle.ContractInitializationProvider;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AetherLink.Contracts.Oracle;

[DependsOn(typeof(MainChainDAppContractTestModule))]
public class OracleContractTestModule : MainChainDAppContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IContractInitializationProvider, OracleContractInitializationProvider>();
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
    }
}