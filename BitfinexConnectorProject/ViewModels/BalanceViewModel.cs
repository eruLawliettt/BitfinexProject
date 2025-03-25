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

        private event Action RecalculationBalance;

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

        private decimal _balanceXrp;
        public decimal BalanceXrp
        {
            get { return _balanceXrp; }
            set => Set(ref _balanceXrp, value, nameof(BalanceXrp));
        }

        private decimal _balanceXmr;
        public decimal BalanceXmr
        {
            get { return _balanceXmr; }
            set => Set(ref _balanceXmr, value, nameof(BalanceXmr));
        }

        private decimal _balanceDash;
        public decimal BalanceDash
        {
            get { return _balanceDash; }
            set => Set(ref _balanceDash, value, nameof(BalanceDash));
        }

        public BalanceViewModel()
        {
            RecalculationBalance += CalculateUSDTBalance;
            RecalculationBalance += CalculateBTCBalance;
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

            CalculateXRPByBTCBalance();
            CalculateXMRByBTCBalance();
            CalculateDASHByBTCBalance();
        }
        private void CalculateXRPByBTCBalance()
        {
            decimal tempBalance = 0M;

            var tempTicker = Tickers.First(t => t.Pair == "tXRPBTCticker");
            tempBalance += BalanceBtc / tempTicker.BidAskAverage;

            BalanceXrp = tempBalance;
        }
        private void CalculateXMRByBTCBalance()
        {
            decimal tempBalance = 0M;

            var tempTicker = Tickers.First(t => t.Pair == "tXMRBTCticker");
            tempBalance += BalanceBtc / tempTicker.BidAskAverage;

            BalanceXmr = tempBalance;
        }
        private void CalculateDASHByBTCBalance()
        {
            decimal tempBalance = 0M;

            var tempTicker = Tickers.First(t => t.Pair == "tDSHBTCticker");
            tempBalance += BalanceBtc / tempTicker.BidAskAverage;

            BalanceDash = tempBalance;
        }


        private async Task WaitForTickersAsync()
        {
            while (Tickers.Count < TickersCount)
            {
                await Task.Delay(100);
            }
        }

        private void AddTickerDTO(TickerDTO tickerDTO)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (Tickers.Any(t => t.Pair == tickerDTO.Pair))
                {
                    var ticker = Tickers.First(t => t.Pair.Equals(tickerDTO.Pair));
                    Tickers.Remove(ticker);
                    Tickers.Add(tickerDTO);
                    RecalculationBalance?.Invoke();
                }
                else
                    Tickers.Add(tickerDTO);
            });
        }


        private async Task InitializeSubscriptionsAsync()
        {
            await Task.Delay(100);
            await _client.ConnectToWebSocketAsync();

            if (_client.IsConnected)
            {
                Subscribe("tBTCUST");
                Subscribe("tXRPUST");
                Subscribe("tXMRUST");
                Subscribe("tDSHUSD");
                Subscribe("tUSTUSD");
                Subscribe("tXRPBTC");
                Subscribe("tXMRBTC");
                Subscribe("tDSHBTC");

                await WaitForTickersAsync();

                CalculateUSDTBalance();
                CalculateBTCBalance();
            }
            else
            {
                await InitializeSubscriptionsAsync();
            }
        }

        // после тестов удалить !!!
        /*private async Task SubscribeAsync(string pair)
        {
            await Task.Delay(100);
            Subscribe(pair);
        }*/

        private void Subscribe(string pair) => _client.SubscribeTickers(pair);
        private void Unsubscribe(string pair) => _client.UnsubscribeTickers(pair);

    }
}
