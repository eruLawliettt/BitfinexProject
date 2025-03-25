using BitfinexConnectorProject.Models;
using BitfinexConnectorProject.Models.DTO;
using BitfinexConnectorProject.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitfinexConnectorProject.ViewModels
{
    class BalanceViewModel : ViewModelBase
    {

        private readonly Client _client = new();

        private const decimal TickersCount = 8;

        private event Action RecalculationUSDTBalance;
        private event Action RecalculationBTCBalance;

        private ObservableCollection<TickerDTO> _tickers;
        public ObservableCollection<TickerDTO> Tickers
        {
            get { return _tickers; }
            set => Set(ref _tickers, value, nameof(Tickers));
        }

        public Dictionary<string, decimal> TestBalance { get; set; } = new () { {"BTC", 1M }, { "XRP", 15_000M }, { "XMR", 50M }, { "DASH", 30M } };

        private decimal _balanceUsdt;
        public decimal BalanceUsdt
        {
            get { return _balanceUsdt; }
            set => Set(ref _balanceUsdt, value, nameof(BalanceUsdt));
        }

        private decimal _balanceBtc;
        public decimal BalanceBtc
        {
            get { return _balanceBtc; }
            set => Set(ref _balanceBtc, value, nameof(BalanceBtc));
        }



        public BalanceViewModel()
        {
            RecalculationUSDTBalance += CalculateUSDTBalance;
            RecalculationBTCBalance += CalculateBTCBalance;
            _client.TickerDTOProcessing += AddTickerDTO;

            Tickers = [];

            _ = InitializeSubscriptionsAsync();
        }

        private void CalculateUSDTBalance()
        {
            decimal tempBalance = 0M;

            var tempTicker = Tickers.First(t => t.Pair == "tBTCUSTticker");            
            tempBalance += TestBalance["BTC"] * tempTicker.BidAskAverage;

            tempTicker = Tickers.First(t => t.Pair == "tXRPUSTticker");
            tempBalance += TestBalance["XRP"] * tempTicker.BidAskAverage;

            tempTicker = Tickers.First(t => t.Pair == "tXMRUSTticker");
            tempBalance += TestBalance["XMR"] * tempTicker.BidAskAverage;

            tempTicker = Tickers.First(t => t.Pair == "tDSHUSDticker");
            decimal usdValue = TestBalance["DASH"] * tempTicker.BidAskAverage;

            tempTicker = Tickers.First(t => t.Pair == "tUSTUSDticker");
            tempBalance += usdValue * tempTicker.BidAskAverage;

            BalanceUsdt = tempBalance;
        }

        private void CalculateBTCBalance()
        {
            decimal tempBalance = 0M;

            tempBalance += TestBalance["BTC"];

            var tempTicker = Tickers.First(t => t.Pair == "tXRPBTCticker");
            tempBalance += TestBalance["XRP"] * tempTicker.BidAskAverage;

            tempTicker = Tickers.First(t => t.Pair == "tXMRBTCticker");
            tempBalance += TestBalance["XMR"] * tempTicker.BidAskAverage;

            tempTicker = Tickers.First(t => t.Pair == "tDSHBTCticker");
            tempBalance += TestBalance["DASH"] * tempTicker.BidAskAverage;

            BalanceBtc = tempBalance;
        }

        private async Task WaitForTickersAsync()
        {
            while (Tickers.Count < TickersCount)
            {
                await Task.Delay(100);
            }
        }

        // USDT - complete
        // BTC - complete
        // XRP 

        // 1 BTC
        // 15_000 XRP
        // 50 XMR
        // 30 DASH

        // Need tickers : tXRPBTC, tXRPXMR does not exist -> recalc via BTC, tXRPDSH does not exist -> recalc via BTC

        // Current price =  ( BID + ASK ) / 2


        private void AddTickerDTO(TickerDTO tickerDTO)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (Tickers.Any(t => t.Pair == tickerDTO.Pair))
                {
                    var ticker = Tickers.First(t => t.Pair.Equals(tickerDTO.Pair));
                    Tickers.Remove(ticker);
                    Tickers.Add(tickerDTO);
                    RecalculationUSDTBalance?.Invoke();
                    RecalculationBTCBalance?.Invoke(); // переделать! оптимизировать!!!
                }
                else
                    Tickers.Add(tickerDTO);
            });
        }


        private async Task InitializeSubscriptionsAsync()
        {
            await SubscribeAsync("tBTCUST");
            await SubscribeAsync("tXRPUST");
            await SubscribeAsync("tXMRUST");
            await SubscribeAsync("tDSHUSD");
            await SubscribeAsync("tUSTUSD");
            await SubscribeAsync("tXRPBTC");
            await SubscribeAsync("tXMRBTC");
            await SubscribeAsync("tDSHBTC");

            await WaitForTickersAsync();

            CalculateUSDTBalance();
            CalculateBTCBalance();
        }
        private async Task SubscribeAsync(string pair)
        {
            Subscribe(pair);
            await Task.Delay(500);
        }
        private void Subscribe(string pair) => _client.SubscribeTickers(pair);
        private void Unsubscribe(string pair) => _client.UnsubscribeTickers(pair);

    }
}
