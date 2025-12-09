using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContractTests : OracleContractTestBase
{
    [Fact]
    public async Task InitializeTests()
    {
        await InitializeAsync();

        {
            var output = await OracleContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var output = await OracleContractStub.GetMaxOracleCount.CallAsync(new Empty());
            output.Value.ShouldBe(32);
        }
        {
            var output = await OracleContractStub.GetSubscriptionConfig.CallAsync(new Empty());
            output.MaxConsumersPerSubscription.ShouldBe(64);
        }
    }

    [Fact]
    public async Task InitializeTests_Fail()
    {
        {
            var result = await UserOracleContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input admin.");
        }

        await OracleContractStub.Initialize.SendAsync(new InitializeInput());

        {
            var output = await OracleContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }

        {
            var result = await OracleContractStub.Initialize.SendWithExceptionAsync(new InitializeInput
            {
                Admin = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Already initialized.");
        }
    }

    [Fact]
    public async Task TransferAdminTests()
    {
        await InitializeAsync();

        {
            var output = await OracleContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var result = await OracleContractStub.TransferAdmin.SendAsync(Accounts[3].Address);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<AdminTransferRequested>(result.TransactionResult);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(Accounts[3].Address);

            var output = await OracleContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(DefaultAddress);
        }
        {
            var result = await OracleContractStub.TransferAdmin.SendAsync(UserAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<AdminTransferRequested>(result.TransactionResult);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(UserAddress);
        }
        {
            var result = await UserOracleContractStub.AcceptAdmin.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<AdminTransferred>(result.TransactionResult);
            log.From.ShouldBe(DefaultAddress);
            log.To.ShouldBe(UserAddress);

            var output = await OracleContractStub.GetAdmin.CallAsync(new Empty());
            output.ShouldBe(UserAddress);
        }
    }

    [Fact]
    public async Task TransferAdminTests_Fail()
    {
        {
            var result = await OracleContractStub.TransferAdmin.SendWithExceptionAsync(UserAddress);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await UserOracleContractStub.AcceptAdmin.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await InitializeAsync();

        {
            var result = await OracleContractStub.TransferAdmin.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input admin.");
        }
        {
            var result = await UserOracleContractStub.TransferAdmin.SendWithExceptionAsync(UserAddress);
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.TransferAdmin.SendWithExceptionAsync(DefaultAddress);
            result.TransactionResult.Error.ShouldContain("Cannot transfer to self.");
        }

        await OracleContractStub.TransferAdmin.SendAsync(UserAddress);

        {
            var result = await OracleContractStub.AcceptAdmin.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
    }

    [Fact]
    public async Task AddCoordinatorTests()
    {
        await InitializeAsync();

        {
            var result = await OracleContractStub.AddCoordinator.SendAsync(DefaultAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<CoordinatorSet>(result.TransactionResult);
            log.CoordinatorContractAddress.ShouldBe(DefaultAddress);
            log.Status.ShouldBe(true);
            log.RequestTypeIndex.ShouldBe(1);
        }

        var firstCoordinator = await OracleContractStub.GetCoordinatorByIndex.CallAsync(new Int32Value
        {
            Value = 1
        });
        firstCoordinator.CoordinatorContractAddress.ShouldBe(DefaultAddress);
        firstCoordinator.Status.ShouldBeTrue();
        firstCoordinator.RequestTypeIndex.ShouldBe(1);

        {
            var coordinators = await OracleContractStub.GetCoordinators.CallAsync(new Empty());
            coordinators.Data.Count.ShouldBe(1);
            coordinators.Data.ShouldBe(new List<Coordinator> { firstCoordinator });
        }
        {
            var result = await OracleContractStub.AddCoordinator.SendAsync(UserAddress);

            var log = GetLogEvent<CoordinatorSet>(result.TransactionResult);
            log.CoordinatorContractAddress.ShouldBe(UserAddress);
            log.Status.ShouldBe(true);
            log.RequestTypeIndex.ShouldBe(2);
        }

        var secondCoordinator = await OracleContractStub.GetCoordinatorByIndex.CallAsync(new Int32Value
        {
            Value = 2
        });
        secondCoordinator.CoordinatorContractAddress.ShouldBe(UserAddress);
        secondCoordinator.Status.ShouldBeTrue();
        secondCoordinator.RequestTypeIndex.ShouldBe(2);

        {
            var coordinators = await OracleContractStub.GetCoordinators.CallAsync(new Empty());
            coordinators.Data.Count.ShouldBe(2);
            coordinators.Data.ShouldBe(new List<Coordinator> { firstCoordinator, secondCoordinator });
        }
        {
            var result = await OracleContractStub.AddCoordinator.SendAsync(DefaultAddress);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<CoordinatorSet>(result.TransactionResult);
            log.CoordinatorContractAddress.ShouldBe(DefaultAddress);
            log.Status.ShouldBe(true);
            log.RequestTypeIndex.ShouldBe(3);
        }

        var sameFirstCoordinator = await OracleContractStub.GetCoordinatorByIndex.CallAsync(new Int32Value
        {
            Value = 3
        });
        sameFirstCoordinator.CoordinatorContractAddress.ShouldBe(firstCoordinator.CoordinatorContractAddress);
        sameFirstCoordinator.Status.ShouldBe(firstCoordinator.Status);
        sameFirstCoordinator.RequestTypeIndex.ShouldBe(3);
        sameFirstCoordinator.ShouldNotBe(firstCoordinator);

        {
            var coordinators = await OracleContractStub.GetCoordinators.CallAsync(new Empty());
            coordinators.Data.Count.ShouldBe(3);
            coordinators.Data.ShouldBe(new List<Coordinator>
                { firstCoordinator, secondCoordinator, sameFirstCoordinator });
        }
    }

    [Fact]
    public async Task AddCoordinatorTests_Fail()
    {
        {
            var result = await OracleContractStub.AddCoordinator.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await InitializeAsync();

        {
            var result = await UserOracleContractStub.AddCoordinator.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.AddCoordinator.SendWithExceptionAsync(new Address());
            result.TransactionResult.Error.ShouldContain("Invalid input.");
        }
    }

    [Fact]
    public async Task SetCoordinatorStatusTests()
    {
        await InitializeAsync();

        await OracleContractStub.AddCoordinator.SendAsync(DefaultAddress);

        var output = await OracleContractStub.GetCoordinatorByIndex.CallAsync(new Int32Value
        {
            Value = 1
        });
        output.Status.ShouldBeTrue();

        {
            var result = await OracleContractStub.SetCoordinatorStatus.SendAsync(new SetCoordinatorStatusInput
            {
                RequestTypeIndex = 1,
                Status = false
            });

            var log = GetLogEvent<CoordinatorSet>(result.TransactionResult);
            log.Status.ShouldBeFalse();
        }
        {
            var result = await OracleContractStub.SetCoordinatorStatus.SendAsync(new SetCoordinatorStatusInput
            {
                RequestTypeIndex = 1,
                Status = false
            });

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name == nameof(CoordinatorSet));
            log.ShouldBeNull();
        }

        output = await OracleContractStub.GetCoordinatorByIndex.CallAsync(new Int32Value
        {
            Value = 1
        });
        output.Status.ShouldBeFalse();
    }

    [Fact]
    public async Task SetCoordinatorStatusTests_Fail()
    {
        {
            var result =
                await OracleContractStub.SetCoordinatorStatus.SendWithExceptionAsync(new SetCoordinatorStatusInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await InitializeAsync();

        {
            var result = await UserOracleContractStub.SetCoordinatorStatus.SendWithExceptionAsync(
                new SetCoordinatorStatusInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.SetCoordinatorStatus.SendWithExceptionAsync(
                new SetCoordinatorStatusInput());
            result.TransactionResult.Error.ShouldContain("Invalid input coordinator type index.");
        }
        {
            var result = await OracleContractStub.SetCoordinatorStatus.SendWithExceptionAsync(
                new SetCoordinatorStatusInput
                {
                    RequestTypeIndex = 1
                });
            result.TransactionResult.Error.ShouldContain("Invalid input coordinator type index.");
        }
    }

    [Fact]
    public async Task SetConfigTests()
    {
        const int f = 1;
        List<Address> signers = new()
            { Signer1Address, Signer2Address, Signer3Address, Signer4Address, Signer5Address };
        List<Address> transmitters = new()
        {
            Transmitter1Address, Transmitter2Address, Transmitter3Address, Transmitter4Address, Transmitter5Address
        };

        await InitializeAsync();

        {
            var result = await OracleContractStub.SetConfig.SendAsync(new SetConfigInput
            {
                F = f,
                Transmitters = { transmitters },
                Signers = { signers }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<ConfigSet>(result.TransactionResult);
            log.PreviousConfigBlockNumber.ShouldBe(0);
            log.ConfigCount.ShouldBe(1);
            log.F.ShouldBe(f);
            log.Signers.Data.ShouldBe(signers);
            log.Transmitters.Data.ShouldBe(transmitters);

            {
                var output = await OracleContractStub.GetConfig.CallAsync(new Empty());
                output.Config.F.ShouldBe(f);
                output.Config.N.ShouldBe(signers.Count);
                output.Config.LatestConfigDigest.ShouldBe(log.ConfigDigest);
                output.Signers.ShouldBe(signers);
                output.Transmitters.ShouldBe(transmitters);
            }
            {
                var output = await OracleContractStub.GetLatestConfigDetails.CallAsync(new Empty());
                output.ConfigDigest.ShouldBe(log.ConfigDigest);
                output.ConfigCount.ShouldBe(1);
                output.BlockNumber.ShouldBe(result.TransactionResult.BlockNumber);
            }
            {
                var output = await OracleContractStub.GetTransmitters.CallAsync(new Empty());
                output.Data.ShouldBe(transmitters);
            }
            {
                var output = await OracleContractStub.GetLatestRound.CallAsync(new Empty());
                output.Value.ShouldBe(0);
            }
            {
                var output = await OracleContractStub.GetOracle.CallAsync(Signer1Address);
                output.Index.ShouldBe(0);
                output.Role.ShouldBe(Role.Signer);
            }
            {
                var output = await OracleContractStub.GetOracle.CallAsync(Transmitter1Address);
                output.Index.ShouldBe(0);
                output.Role.ShouldBe(Role.Transmitter);
            }
        }
        {
            var result = await OracleContractStub.SetConfig.SendAsync(new SetConfigInput
            {
                F = f,
                Transmitters = { transmitters },
                Signers = { signers }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var output = await OracleContractStub.GetLatestConfigDetails.CallAsync(new Empty());
            output.ConfigCount.ShouldBe(2);
        }
    }

    [Fact]
    public async Task SetConfigTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await UserOracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput());
            result.TransactionResult.Error.ShouldContain("f must be positive.");
        }
        {
            var result = await OracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput
            {
                F = 1
            });
            result.TransactionResult.Error.ShouldContain("Faulty-oracle f too high.");
        }
        {
            {
                var result = await OracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput
                {
                    F = 1,
                    Signers = { Accounts[1].Address, Accounts[2].Address },
                    Transmitters = { Accounts[3].Address }
                });
                result.TransactionResult.Error.ShouldContain("Oracle addresses out of registration.");
            }
        }
        {
            var result = await OracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput
            {
                F = 1,
                Signers = { Accounts[1].Address, Accounts[1].Address, Accounts[3].Address, Accounts[4].Address },
                Transmitters =
                    { Accounts[2].Address, Accounts[2].Address, Accounts[3].Address, Accounts[4].Address }
            });
            result.TransactionResult.Error.ShouldContain("Repeated signer address.");
        }
        {
            var result = await OracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput
            {
                F = 1,
                Signers = { Accounts[1].Address, Accounts[2].Address, Accounts[3].Address, Accounts[4].Address },
                Transmitters =
                    { Accounts[5].Address, Accounts[5].Address, Accounts[7].Address, Accounts[8].Address }
            });
            result.TransactionResult.Error.ShouldContain("Repeated transmitter address.");
        }
        {
            var result = await OracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput
            {
                F = 1,
                Signers = { Accounts[1].Address, Accounts[2].Address, Accounts[3].Address, Accounts[4].Address },
                Transmitters =
                    { Accounts[1].Address, Accounts[2].Address, Accounts[3].Address, Accounts[4].Address }
            });
            result.TransactionResult.Error.ShouldContain("Repeated transmitter address.");
        }
        {
            var result = await OracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput
            {
                F = 1,
                Signers = { new Address(), Accounts[2].Address, Accounts[3].Address, Accounts[4].Address },
                Transmitters =
                    { Accounts[5].Address, Accounts[6].Address, Accounts[7].Address, Accounts[8].Address }
            });
            result.TransactionResult.Error.ShouldContain("Invalid signer address.");
        }
        {
            var result = await OracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput
            {
                F = 1,
                Signers = { Accounts[1].Address, Accounts[2].Address, Accounts[3].Address, Accounts[4].Address },
                Transmitters = { new Address(), Accounts[6].Address, Accounts[7].Address, Accounts[8].Address }
            });
            result.TransactionResult.Error.ShouldContain("Invalid transmitter address.");
        }
        {
            await OracleContractStub.SetMaxOracleCount.SendAsync(new Int64Value
            {
                Value = 1
            });
            var result = await OracleContractStub.SetConfig.SendWithExceptionAsync(new SetConfigInput
            {
                F = 1,
                Signers = { Accounts[1].Address, Accounts[2].Address },
                Transmitters = { Accounts[3].Address, Accounts[4].Address }
            });
            result.TransactionResult.Error.ShouldContain("Too many signers.");
        }
    }

    [Fact]
    public async Task SetMaxOracleCountTests()
    {
        const long maxOracleCount = 32;
        const long newMaxOracleCount = long.MaxValue;

        await InitializeAsync();

        {
            var output = await OracleContractStub.GetMaxOracleCount.CallAsync(new Empty());
            output.Value.ShouldBe(maxOracleCount);
        }

        {
            var result = await OracleContractStub.SetMaxOracleCount.SendAsync(new Int64Value
            {
                Value = newMaxOracleCount
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var output = await OracleContractStub.GetMaxOracleCount.CallAsync(new Empty());
            output.Value.ShouldBe(newMaxOracleCount);
        }
    }

    [Fact]
    public async Task SetMaxOracleCountTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await UserOracleContractStub.SetMaxOracleCount.SendWithExceptionAsync(new Int64Value
            {
                Value = 50
            });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.SetMaxOracleCount.SendWithExceptionAsync(new Int64Value
            {
                Value = 0
            });
            result.TransactionResult.Error.ShouldContain("Must be positive.");
        }
    }

    [Fact]
    public async Task RegisterProvingKeyTests()
    {
        var publicProvingKey = UserKeyPair.PublicKey.ToHex();
        var hash = await OracleContractStub.GetHashFromKey.CallAsync(new StringValue
        {
            Value = publicProvingKey
        });

        await InitializeAsync();

        {
            var output = await OracleContractStub.GetProvingKeyHashes.CallAsync(new Empty());
            output.Data.Count.ShouldBe(0);
        }

        {
            var result = await OracleContractStub.RegisterProvingKey.SendAsync(new RegisterProvingKeyInput
            {
                Oracle = UserAddress,
                PublicProvingKey = publicProvingKey
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<ProvingKeyRegistered>(result.TransactionResult);
            log.Oracle.ShouldBe(UserAddress);
            log.KeyHash.ShouldBe(hash);
        }
        {
            var output = await OracleContractStub.GetProvingKeyHashes.CallAsync(new Empty());
            output.Data.Count.ShouldBe(1);
            output.Data.First().ShouldBe(hash);
        }
        {
            var output = await OracleContractStub.GetOracleByProvingKeyHash.CallAsync(new StringValue
            {
                Value = publicProvingKey
            });
            output.ShouldBe(UserAddress);
        }
        {
            var result = await OracleContractStub.RegisterProvingKey.SendAsync(new RegisterProvingKeyInput
            {
                Oracle = UserAddress,
                PublicProvingKey = publicProvingKey
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name == nameof(ProvingKeyRegistered));
            log.ShouldBeNull();
        }
        {
            var output = await OracleContractStub.GetProvingKeyHashes.CallAsync(new Empty());
            output.Data.Count.ShouldBe(1);
        }
        {
            var result = await OracleContractStub.DeregisterProvingKey.SendAsync(new DeregisterProvingKeyInput
            {
                PublicProvingKey = publicProvingKey
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<ProvingKeyDeregistered>(result.TransactionResult);
            log.Oracle.ShouldBe(UserAddress);
            log.KeyHash.ShouldBe(hash);
        }
        {
            var output = await OracleContractStub.GetProvingKeyHashes.CallAsync(new Empty());
            output.Data.Count.ShouldBe(0);
        }
        {
            var output = await OracleContractStub.GetOracleByProvingKeyHash.CallAsync(new StringValue
            {
                Value = publicProvingKey
            });
            output.ShouldBe(new Address());
        }
        {
            var result = await OracleContractStub.DeregisterProvingKey.SendAsync(new DeregisterProvingKeyInput
            {
                PublicProvingKey = publicProvingKey
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = result.TransactionResult.Logs.FirstOrDefault(l => l.Name == nameof(ProvingKeyDeregistered));
            log.ShouldBeNull();
        }
    }

    [Fact]
    public async Task RegisterProvingKeyTests_Fail()
    {
        await InitializeAsync();

        {
            var result =
                await UserOracleContractStub.RegisterProvingKey.SendWithExceptionAsync(
                    new RegisterProvingKeyInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result =
                await OracleContractStub.RegisterProvingKey.SendWithExceptionAsync(new RegisterProvingKeyInput());
            result.TransactionResult.Error.ShouldContain("Invalid input oracle.");
        }
        {
            var result = await OracleContractStub.RegisterProvingKey.SendWithExceptionAsync(
                new RegisterProvingKeyInput
                {
                    Oracle = new Address()
                });
            result.TransactionResult.Error.ShouldContain("Invalid input oracle.");
        }
        {
            var result = await OracleContractStub.RegisterProvingKey.SendWithExceptionAsync(
                new RegisterProvingKeyInput
                {
                    Oracle = UserAddress
                });
            result.TransactionResult.Error.ShouldContain("Invalid input public proving key.");
        }
        {
            var result = await OracleContractStub.RegisterProvingKey.SendWithExceptionAsync(
                new RegisterProvingKeyInput
                {
                    Oracle = UserAddress,
                    PublicProvingKey = ""
                });
            result.TransactionResult.Error.ShouldContain("Invalid input public proving key.");
        }
        await OracleContractStub.RegisterProvingKey.SendAsync(new RegisterProvingKeyInput
        {
            Oracle = UserAddress,
            PublicProvingKey = UserKeyPair.PublicKey.ToHex()
        });
        {
            var result =
                await UserOracleContractStub.DeregisterProvingKey.SendWithExceptionAsync(
                    new DeregisterProvingKeyInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result =
                await OracleContractStub.DeregisterProvingKey.SendWithExceptionAsync(
                    new DeregisterProvingKeyInput());
            result.TransactionResult.Error.ShouldContain("Invalid input public proving key.");
        }
    }

    [Fact]
    public async Task PauseTests()
    {
        await InitializeAsync();

        {
            var result = await OracleContractStub.Pause.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<Paused>(result.TransactionResult);
            log.Account.ShouldBe(DefaultAddress);
        }
        {
            var output = await OracleContractStub.IsPaused.CallAsync(new Empty());
            output.Value.ShouldBeTrue();
        }
        {
            var result = await OracleContractStub.CreateSubscription.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
        {
            var result = await OracleContractStub.Unpause.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var log = GetLogEvent<Unpaused>(result.TransactionResult);
            log.Account.ShouldBe(DefaultAddress);
        }
        {
            var output = await OracleContractStub.IsPaused.CallAsync(new Empty());
            output.Value.ShouldBeFalse();
        }
        {
            var result = await OracleContractStub.CreateSubscription.SendAsync(new Empty());
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    [Fact]
    public async Task PauseTests_Fail()
    {
        await InitializeAsync();

        {
            var result = await OracleContractStub.Unpause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Contract not on pause.");
        }
        {
            var result = await UserOracleContractStub.Pause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }

        await PauseAsync();

        {
            var result = await OracleContractStub.Pause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Already paused.");
        }
        {
            var result = await UserOracleContractStub.Unpause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        await OracleContractStub.Unpause.SendAsync(new Empty());
        {
            var result = await OracleContractStub.Unpause.SendWithExceptionAsync(new Empty());
            result.TransactionResult.Error.ShouldContain("Contract not on pause.");
        }
    }
}