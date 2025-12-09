using System.Linq;
using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using AetherLink.Contracts.Oracle;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.Consumer;

public partial class ConsumerContract : ConsumerContractContainer.ConsumerContractBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        Assert(input != null, "Invalid input.");

        State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
        Assert(State.GenesisContract.GetContractInfo.Call(Context.Self).Deployer == Context.Sender, "No permission.");

        Assert(input.Admin == null || !input.Admin.Value.IsNullOrEmpty(), "Invalid input admin.");
        State.Admin.Value = input.Admin ?? Context.Sender;

        Assert(input.Oracle == null || !input.Oracle.Value.IsNullOrEmpty(), "Invalid input oracle.");
        if (input.Oracle != null)
        {
            State.OracleContract.Value = input.Oracle;
        }

        State.Controller.Value = new AddressList
        {
            Data = { State.Admin.Value }
        };
        State.DataFeedsRequestTypeIndex.Value = input.DataFeedsRequestTypeIndex;
        State.VrfRequestTypeIndex.Value = input.VrfRequestTypeIndex;
        State.Initialized.Value = true;

        return new Empty();
    }

    public override Empty TransferAdmin(Address input)
    {
        CheckAdminPermission();
        Assert(IsAddressValid(input), "Invalid input admin.");
        Assert(input != State.Admin.Value, "Cannot transfer to self.");

        State.PendingAdmin.Value = input;

        Context.Fire(new AdminTransferRequested
        {
            From = Context.Sender,
            To = input
        });

        return new Empty();
    }

    public override Empty AcceptAdmin(Empty input)
    {
        Assert(Context.Sender == State.PendingAdmin.Value, "No permission.");

        var from = State.Admin.Value.Clone();

        State.Admin.Value = Context.Sender;
        State.PendingAdmin.Value = new Address();

        Context.Fire(new AdminTransferred
        {
            From = from,
            To = Context.Sender
        });

        return new Empty();
    }

    public override Empty AddController(AddressList input)
    {
        CheckAdminPermission();

        Assert(input != null && input.Data.Count > 0, "Invalid input.");

        State.Controller.Value ??= new AddressList();

        var list = input.Data.Distinct().Except(State.Controller.Value.Data).ToList();
        if (list.Count == 0)
        {
            return new Empty();
        }

        State.Controller.Value.Data.AddRange(list);

        Context.Fire(new ControllerAdded
        {
            Controllers = new AddressList
            {
                Data = { list }
            }
        });

        return new Empty();
    }

    public override Empty RemoveController(AddressList input)
    {
        CheckAdminPermission();

        Assert(input != null && input.Data.Count > 0, "Invalid input.");

        var list = input.Data.Distinct().Intersect(State.Controller.Value.Data).ToList();

        if (list.Count == 0)
        {
            return new Empty();
        }

        foreach (var address in list)
        {
            State.Controller.Value.Data.Remove(address);
        }

        Context.Fire(new ControllerRemoved
        {
            Controllers = new AddressList
            {
                Data = { list }
            }
        });

        return new Empty();
    }

    public override Address GetAdmin(Empty input)
    {
        return State.Admin.Value;
    }

    public override AddressList GetController(Empty input)
    {
        return State.Controller.Value;
    }

    public override Empty StartOracleRequest(StartOracleRequestInput input)
    {
        CheckControllerPermission();
        Assert(State.OracleContract.Value != null, "Oracle contract not set.");
        Assert(input != null, "Invalid input.");
        Assert(input.SubscriptionId > 0, "Invalid input subscription id.");
        Assert(input.RequestTypeIndex > 0, "Invalid request type index.");

        State.OracleContract.SendRequest.Send(new SendRequestInput
        {
            SubscriptionId = input.SubscriptionId,
            RequestTypeIndex = input.RequestTypeIndex,
            TraceId = input.TraceId,
            SpecificData = input.SpecificData
        });

        Context.Fire(new OracleRequestStarted
        {
            SubscriptionId = input.SubscriptionId,
            RequestTypeIndex = input.RequestTypeIndex,
            SpecificData = input.SpecificData
        });

        return new Empty();
    }

    public override Empty HandleOracleFulfillment(HandleOracleFulfillmentInput input)
    {
        CheckOraclePermission();
        Assert(input != null, "Invalid input.");
        Assert(IsHashValid(input.RequestId), "Invalid input request id.");
        Assert(input.RequestTypeIndex > 0, "Invalid request type index.");
        Assert(!input.Response.IsNullOrEmpty() || !input.Err.IsNullOrEmpty(), "Invalid input response or err.");

        if (input.RequestTypeIndex == State.DataFeedsRequestTypeIndex.Value)
        {
            FulfillDataFeedsRequest(input);
        }
        else if (input.RequestTypeIndex == State.VrfRequestTypeIndex.Value)
        {
            FulfillVrfRequest(input);
        }
        else
        {
            Assert(false, "Invalid request type index.");
        }

        Context.Fire(new OracleFulfillmentHandled
        {
            RequestId = input.RequestId,
            RequestTypeIndex = input.RequestTypeIndex
        });

        return new Empty();
    }

    private void FulfillDataFeedsRequest(HandleOracleFulfillmentInput input)
    {
        State.OracleResponses[input.RequestId] = new OracleResponse
        {
            Response = input.Response,
            Err = input.Err
        };

        if (input.Response.IsNullOrEmpty()) return;

        var priceList = LongList.Parser.ParseFrom(input.Response);

        var longList = new LongList
        {
            Data = { priceList.Data }
        };

        State.Prices[input.RequestId] = longList;

        var sortedList = longList.Data.ToList().OrderBy(l => l).ToList();

        var from = State.LatestPriceRoundData.Value;

        var round = State.LatestRound.Value.Add(1);
        var newPriceRoundData = new PriceRoundData
        {
            Price = sortedList[sortedList.Count / 2],
            RoundId = round,
            UpdatedAt = Context.CurrentBlockTime
        };
        State.LatestPriceRoundData.Value = newPriceRoundData;
        State.PriceRoundData[round] = newPriceRoundData;
        State.LatestRound.Value = round;

        Context.Fire(new PriceUpdated
        {
            From = from?.Price ?? 0,
            To = newPriceRoundData.Price,
            RoundNumber = round,
            UpdateAt = Context.CurrentBlockTime
        });
    }

    private void FulfillVrfRequest(HandleOracleFulfillmentInput input)
    {
        State.OracleResponses[input.RequestId] = new OracleResponse
        {
            Response = input.Response,
            Err = input.Err
        };

        if (input.Response.IsNullOrEmpty()) return;

        var randomHashList = HashList.Parser.ParseFrom(input.Response);
        State.RandomHashes[input.RequestId] = randomHashList;
    }

    public override Empty SetOracleContract(Address input)
    {
        CheckAdminPermission();
        Assert(IsAddressValid(input), "Invalid input.");

        State.OracleContract.Value = input;

        return new Empty();
    }

    public override Address GetOracleContract(Empty input)
    {
        return State.OracleContract.Value;
    }

    public override OracleResponse GetOracleResponse(Hash input)
    {
        return State.OracleResponses[input];
    }

    public override PriceRoundData GetLatestPriceRoundData(Empty input)
    {
        return State.LatestPriceRoundData.Value;
    }

    public override LongList GetPriceList(Hash input)
    {
        return State.Prices[input];
    }

    public override HashList GetRandomHashList(Hash input)
    {
        return State.RandomHashes[input];
    }

    public override Int64Value GetLatestRound(Empty input)
    {
        return new Int64Value
        {
            Value = State.LatestRound.Value
        };
    }

    public override PriceRoundData GetPriceRoundData(Int64Value input)
    {
        return State.PriceRoundData[input.Value];
    }

    public override Empty SetDataFeedsRequestTypeIndex(Int32Value input)
    {
        CheckAdminPermission();
        Assert(input != null && input.Value > 0, "Invalid input.");

        State.DataFeedsRequestTypeIndex.Value = input.Value;

        return new Empty();
    }

    public override Int32Value GetDataFeedsRequestTypeIndex(Empty input)
    {
        return new Int32Value
        {
            Value = State.DataFeedsRequestTypeIndex.Value
        };
    }

    public override Empty SetVrfRequestTypeIndex(Int32Value input)
    {
        CheckAdminPermission();
        Assert(input != null && input.Value > 0, "Invalid input.");

        State.VrfRequestTypeIndex.Value = input.Value;

        return new Empty();
    }

    public override Int32Value GetVrfRequestTypeIndex(Empty input)
    {
        return new Int32Value
        {
            Value = State.VrfRequestTypeIndex.Value
        };
    }
}