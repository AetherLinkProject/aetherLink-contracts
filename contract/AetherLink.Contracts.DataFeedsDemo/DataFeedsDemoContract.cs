using System.Linq;
using AElf;
using AElf.Contracts.DataFeedsDemo;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using AetherLink.Contracts.Consumer;
using AetherLink.Contracts.Oracle;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AetherLink.Contracts.DataFeedsDemo;

public class DataFeedsDemoContract : DataFeedsDemoContractContainer.DataFeedsDemoContractBase
{
    private const long ELFUSDTInitPrice = 600000000;
    private const long SGRUSDTInitPrice = 600000000;
    private const string ELFUSDTTokenPair = "ELF/USDT";
    private const string SGRUSDTTokenPair = "SGR/USDT";
    private const string ELFPaymentTokenName = "ELF";
    private const long SubscriptionId = 1; // input your subscriptionId
    private const int RequestTypeIndex = 1;

    private const string
        OracleContractAddress = "21Fh7yog1B741yioZhNAFbs3byJ97jvBmbGAPPZKZpHHog5aEg"; // tDVW oracle contract address

    private const string ELFUSDTJobSpec =
        "{\"Cron\": \"0 */1 * * * ?\",\"DataFeedsJobSpec\": {\"Type\": \"PriceFeeds\",\"CurrencyPair\": \"ELF/USDT\"}}";

    private const string SGRUSDTJobSpec =
        "{\"Cron\": \"0 */1 * * * ?\",\"DataFeedsJobSpec\": {\"Type\": \"PriceFeeds\",\"CurrencyPair\": \"SGR/USDT\"}}";

    // Initializes the contract
    public override Empty Initialize(Empty input)
    {
        Assert(State.Initialized.Value == false, "Already initialized.");
        State.Initialized.Value = true;
        State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        State.OracleContract.Value = Address.FromBase58(OracleContractAddress);
        return new Empty();
    }

    public override Empty StartPriceCollection(Empty input)
    {
        #region Start elf-usdt price request

        {
            var elfSpecData = new AetherLink.Contracts.DataFeeds.Coordinator.SpecificData
            {
                Data = ByteString.CopyFromUtf8(ELFUSDTJobSpec),
                DataVersion = 0
            }.ToByteString();
            var elfPriceRequestInput = new SendRequestInput
            {
                SubscriptionId = SubscriptionId,
                RequestTypeIndex = RequestTypeIndex,
                SpecificData = elfSpecData
            };
            var elfTraceId = HashHelper.ComputeFrom(elfPriceRequestInput);
            elfPriceRequestInput.TraceId = elfTraceId;
            State.OracleContract.SendRequest.Send(elfPriceRequestInput);
            State.PriceDataMap[elfTraceId] = new() { Price = ELFUSDTInitPrice, TokenPair = ELFUSDTTokenPair };
        }

        #endregion

        #region Start sgr-usdt price request

        {
            var sgrSpecData = new AetherLink.Contracts.DataFeeds.Coordinator.SpecificData
            {
                Data = ByteString.CopyFromUtf8(SGRUSDTJobSpec),
                DataVersion = 0
            }.ToByteString();
            var sgrPriceRequestInput = new SendRequestInput
            {
                SubscriptionId = SubscriptionId,
                RequestTypeIndex = RequestTypeIndex,
                SpecificData = sgrSpecData
            };
            var sgrTraceId = HashHelper.ComputeFrom(sgrPriceRequestInput);
            sgrPriceRequestInput.TraceId = sgrTraceId;
            State.OracleContract.SendRequest.Send(sgrPriceRequestInput);
            State.PriceDataMap[sgrTraceId] = new() { Price = SGRUSDTInitPrice, TokenPair = SGRUSDTTokenPair };
        }

        #endregion

        return new Empty();
    }

    public override Empty HandleOracleFulfillment(HandleOracleFulfillmentInput input)
    {
        if (input.Response.IsNullOrEmpty()) return new Empty();
        if (input.TraceId == null || State.PriceDataMap[input.TraceId] == null) return new Empty();
        var priceList = LongList.Parser.ParseFrom(input.Response);
        var longList = new LongList { Data = { priceList.Data } };
        var sortedList = longList.Data.ToList().OrderBy(l => l).ToList();
        var latestPrice = sortedList[sortedList.Count / 2];
        State.PriceDataMap[input.TraceId].Price = latestPrice;

        Context.Fire(new PriceUpdated
        {
            Price = latestPrice,
            TokenPair = State.PriceDataMap[input.TraceId].TokenPair,
            UpdateAt = Context.CurrentBlockTime
        });

        return new Empty();
    }

    // transfer nft
    public override Empty Purchase(PurchaseInput input)
    {
        // The price of NFT assets, assuming it is 10U
        var price = State.NftPrice.Value;
        
        // Receive ELF paid by the buyer
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = Context.Sender,
            To = Context.Self,
            Symbol = ELFPaymentTokenName,
            // 10 / 0.033 = 10U / (1U / 30ELF)
            Amount = price.Amount.Div(State.PriceDataMap[price.Symbol].Price)
        });

        // Transfer NFT assets to buyers
        State.TokenContract.Transfer.Send(new TransferInput
        {
            To = Context.Sender,
            Symbol = input.TokenSymbolToBuy,
            Amount = input.TokenAmount
        });

        return new Empty();
    }
}