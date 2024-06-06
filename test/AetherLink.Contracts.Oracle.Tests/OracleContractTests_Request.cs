using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Oracle;
using Shouldly;
using Xunit;

namespace AetherLink.Contracts.Oracle;

public partial class OracleContractTests
{
    [Fact]
    public async Task StartRequestTests_Fail()
    {
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }
        await OracleContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput());
            result.TransactionResult.Error.ShouldContain("Unauthorized coordinator contract.");
        }

        await OracleContractStub.AddCoordinator.SendAsync(DefaultAddress);

        {
            var result = await UserOracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input request id.");
        }
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1,
                RequestId = new Hash()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input request id.");
        }
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1,
                RequestId = HashHelper.ComputeFrom("RequestId")
            });
            result.TransactionResult.Error.ShouldContain("Invalid input requesting contract.");
        }
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1,
                RequestId = HashHelper.ComputeFrom("RequestId"),
                RequestingContract = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input requesting contract.");
        }
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1,
                RequestId = HashHelper.ComputeFrom("RequestId"),
                RequestingContract = DefaultAddress,
            });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription id.");
        }
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1,
                RequestId = HashHelper.ComputeFrom("RequestId"),
                RequestingContract = DefaultAddress,
                SubscriptionId = 1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription owner.");
        }
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1,
                RequestId = HashHelper.ComputeFrom("RequestId"),
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                SubscriptionOwner = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription owner.");
        }
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1,
                RequestId = HashHelper.ComputeFrom("RequestId"),
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                SubscriptionOwner = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid input commitment.");
        }
        {
            var result = await OracleContractStub.StartRequest.SendWithExceptionAsync(new StartRequestInput
            {
                RequestTypeIndex = 1,
                RequestId = HashHelper.ComputeFrom("RequestId"),
                RequestingContract = DefaultAddress,
                SubscriptionId = 1,
                SubscriptionOwner = DefaultAddress,
                Commitment = ByteString.Empty
            });
            result.TransactionResult.Error.ShouldContain("Invalid input commitment.");
        }
    }

    [Fact]
    public async Task TransmitTests_Fail()
    {
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }
        await OracleContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput());
            result.TransactionResult.Error.ShouldContain("Not transmitter.");
        }

        await OracleContractStub.SetConfig.SendAsync(new SetConfigInput
        {
            F = 1,
            Signers = { Accounts[1].Address, Accounts[2].Address, Accounts[3].Address, Accounts[4].Address },
            Transmitters = { DefaultAddress, Accounts[5].Address, Accounts[6].Address, Accounts[7].Address }
        });

        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput());
            result.TransactionResult.Error.ShouldContain("Invalid input report context.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { new Hash() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input report context.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { new Hash(), new Hash(), new Hash() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input config digest.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { HashHelper.ComputeFrom(1), new Hash(), new Hash() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input epochAndRound.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { HashHelper.ComputeFrom(1), HashHelper.ComputeFrom(2), new Hash() }
            });
            result.TransactionResult.Error.ShouldContain("Invalid input report.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { HashHelper.ComputeFrom(1), HashHelper.ComputeFrom(2), new Hash() },
                Report = ByteString.Empty
            });
            result.TransactionResult.Error.ShouldContain("Invalid input report.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { HashHelper.ComputeFrom(1), HashHelper.ComputeFrom(2), new Hash() },
                Report = HashHelper.ComputeFrom(1).Value
            });
            result.TransactionResult.Error.ShouldContain("Invalid input signature.");
        }
        {
            var result = await OracleContractStub.Transmit.SendWithExceptionAsync(new TransmitInput
            {
                ReportContext = { HashHelper.ComputeFrom(1), HashHelper.ComputeFrom(2), new Hash() },
                Report = HashHelper.ComputeFrom(1).Value,
                Signatures = { HashHelper.ComputeFrom(1).Value }
            });
            result.TransactionResult.Error.ShouldContain("Config digest mismatch.");
        }
    }

    [Fact]
    public async Task SendRequestTests_Fail()
    {
        {
            var result = await OracleContractStub.SendRequest.SendWithExceptionAsync(new SendRequestInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }
        await OracleContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        {
            var result = await OracleContractStub.SendRequest.SendWithExceptionAsync(new SendRequestInput());
            result.TransactionResult.Error.ShouldContain("Invalid subscription id.");
        }
        {
            var result = await OracleContractStub.SendRequest.SendWithExceptionAsync(new SendRequestInput
            {
                SubscriptionId = 1,
            });
            result.TransactionResult.Error.ShouldContain("Invalid request type index.");
        }
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);
        {
            var result = await OracleContractStub.SendRequest.SendWithExceptionAsync(new SendRequestInput
            {
                SubscriptionId = 2,
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);
        {
            var result = await UserOracleContractStub.SendRequest.SendWithExceptionAsync(new SendRequestInput
            {
                SubscriptionId = 1,
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("Consumer not found in subscription.");
        }
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);
        {
            var result = await OracleContractStub.SendRequest.SendWithExceptionAsync(new SendRequestInput
            {
                SubscriptionId = 1,
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("Coordinator not found.");
        }
        await OracleContractStub.AddCoordinator.SendAsync(DefaultAddress);
        await OracleContractStub.SetCoordinatorStatus.SendAsync(new SetCoordinatorStatusInput
        {
            Status = false,
            RequestTypeIndex = 1
        });
        {
            var result = await OracleContractStub.SendRequest.SendWithExceptionAsync(new SendRequestInput
            {
                SubscriptionId = 1,
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("Coordinator not available.");
        }
        await OracleContractStub.Pause.SendAsync(new Empty());
        {
            var result = await OracleContractStub.SendRequest.SendWithExceptionAsync(new SendRequestInput());
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
    }

    [Fact]
    public async Task FulfillTests_Fail()
    {
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }
        await OracleContractStub.Initialize.SendAsync(new InitializeInput
        {
            Admin = DefaultAddress
        });
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput());
            result.TransactionResult.Error.ShouldContain("Invalid transmitter.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid transmitter.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
            });
            result.TransactionResult.Error.ShouldContain("Invalid response or err.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value
            });
            result.TransactionResult.Error.ShouldContain("Invalid commitment.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment()
            });
            result.TransactionResult.Error.ShouldContain("Invalid commitment request id.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid commitment coordinator.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty,
                    Coordinator = DefaultAddress
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid commitment client.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty,
                    Coordinator = DefaultAddress,
                    Client = DefaultAddress
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid commitment subscription id.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty,
                    Coordinator = DefaultAddress,
                    Client = DefaultAddress,
                    SubscriptionId = 1
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid commitment timeout timestamp.");
        }
        {
            var result = await UserOracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty,
                    Coordinator = DefaultAddress,
                    Client = DefaultAddress,
                    SubscriptionId = 1,
                    TimeoutTimestamp = new Timestamp()
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid commitment request type index.");
        }
        {
            var result = await UserOracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty,
                    Coordinator = DefaultAddress,
                    Client = DefaultAddress,
                    SubscriptionId = 1,
                    TimeoutTimestamp = new Timestamp(),
                    RequestTypeIndex = 1
                }
            });
            result.TransactionResult.Error.ShouldContain("Commitment mismatches.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty,
                    Coordinator = DefaultAddress,
                    Client = DefaultAddress,
                    SubscriptionId = 1,
                    TimeoutTimestamp = new Timestamp(),
                    RequestTypeIndex = 1
                }
            });
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }
        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty,
                    Coordinator = DefaultAddress,
                    Client = UserAddress,
                    SubscriptionId = 1,
                    TimeoutTimestamp = new Timestamp(),
                    RequestTypeIndex = 1
                }
            });
            result.TransactionResult.Error.ShouldContain("Consumer not found in subscription.");
        }
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty,
                    Coordinator = DefaultAddress,
                    Client = DefaultAddress,
                    SubscriptionId = 1,
                    TimeoutTimestamp = new Timestamp(),
                    RequestTypeIndex = 1
                }
            });
            result.TransactionResult.Error.ShouldContain("Coordinator not available.");
        }
        await OracleContractStub.AddCoordinator.SendAsync(DefaultAddress);
        await OracleContractStub.SetCoordinatorStatus.SendAsync(new SetCoordinatorStatusInput
        {
            Status = false,
            RequestTypeIndex = 1
        });
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput
            {
                Transmitter = DefaultAddress,
                Response = Hash.Empty.Value,
                Err = Hash.Empty.Value,
                Commitment = new Commitment
                {
                    RequestId = Hash.Empty,
                    Coordinator = DefaultAddress,
                    Client = DefaultAddress,
                    SubscriptionId = 1,
                    TimeoutTimestamp = new Timestamp(),
                    RequestTypeIndex = 1
                }
            });
            result.TransactionResult.Error.ShouldContain("Coordinator not available.");
        }
        await OracleContractStub.Pause.SendAsync(new Empty());
        {
            var result = await OracleContractStub.Fulfill.SendWithExceptionAsync(new FulfillInput());
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
    }

    [Fact]
    public async Task CancelRequestTests_Fail()
    {
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput());
            result.TransactionResult.Error.ShouldContain("Not initialized.");
        }

        await InitializeAsync();

        {
            var result = await UserOracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput());
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput());
            result.TransactionResult.Error.ShouldContain("Invalid input request id.");
        }
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput
            {
                RequestId = new Hash()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input request id.");
        }
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput
            {
                RequestId = Hash.Empty
            });
            result.TransactionResult.Error.ShouldContain("Invalid input subscription id.");
        }
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput
            {
                RequestId = Hash.Empty,
                SubscriptionId = 1
            });
            result.TransactionResult.Error.ShouldContain("Invalid input consumer.");
        }
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput
            {
                RequestId = Hash.Empty,
                SubscriptionId = 1,
                Consumer = new Address()
            });
            result.TransactionResult.Error.ShouldContain("Invalid input consumer.");
        }
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput
            {
                RequestId = Hash.Empty,
                SubscriptionId = 1,
                Consumer = DefaultAddress
            });
            result.TransactionResult.Error.ShouldContain("Invalid input request type index.");
        }
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput
            {
                RequestId = Hash.Empty,
                SubscriptionId = 1,
                Consumer = DefaultAddress,
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("Subscription not found.");
        }

        await OracleContractStub.CreateSubscriptionWithConsumer.SendAsync(DefaultAddress);

        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput
            {
                RequestId = Hash.Empty,
                SubscriptionId = 1,
                Consumer = UserAddress,
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("Consumer not found in subscription.");
        }
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput
            {
                RequestId = Hash.Empty,
                SubscriptionId = 1,
                Consumer = DefaultAddress,
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("Coordinator not found.");
        }

        await OracleContractStub.AddCoordinator.SendAsync(DefaultAddress);
        await OracleContractStub.SetCoordinatorStatus.SendAsync(new SetCoordinatorStatusInput
        {
            Status = false,
            RequestTypeIndex = 1
        });
        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput
            {
                RequestId = Hash.Empty,
                SubscriptionId = 1,
                Consumer = DefaultAddress,
                RequestTypeIndex = 1
            });
            result.TransactionResult.Error.ShouldContain("Coordinator not available.");
        }

        await PauseAsync();

        {
            var result = await OracleContractStub.CancelRequest.SendWithExceptionAsync(new CancelRequestInput());
            result.TransactionResult.Error.ShouldContain("Contract paused.");
        }
    }
}