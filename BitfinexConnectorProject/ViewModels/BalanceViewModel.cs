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

        private event Action RecalculationUSDTBalance;

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


        public BalanceViewModel()
        {
            RecalculationUSDTBalance += CalculateUSDTBalance;
            _client.TickerDTOProcessing += AddTickerDTO;

            Tickers = [];

            _ = InitializeSubscriptionsAsync();
        }

        private void CalculateUSDTBalance()
        {
            decimal balance = 0M;
            TickerDTO ticker = Tickers.First(t => t.Pair == "tBTCUSTticker");            
            balance += TestBalance["BTC"] * ticker.BidAskAverage;

            ticker = Tickers.First(t => t.Pair == "tXRPUSTticker");
            balance += TestBalance["XRP"] * ticker.BidAskAverage;

            ticker = Tickers.First(t => t.Pair == "tXMRUSTticker");
            balance += TestBalance["XMR"] * ticker.BidAskAverage;

            ticker = Tickers.First(t => t.Pair == "tDSHUSDticker");
            decimal usdValue = TestBalance["DASH"] * ticker.BidAskAverage;

            ticker = Tickers.First(t => t.Pair == "tUSTUSDticker");
            balance += usdValue * ticker.BidAskAverage;

            BalanceUsdt = balance;
        }

        private async Task WaitForTickersAsync()
        {
            while (Tickers.Count < 5)
            {
                await Task.Delay(100);
            }
        }

        //USDT - complete

        // 1 BTC
        // 15_000 XRP
        // 50 XMR
        // 30 DASH

        // Need tickers : 

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

            await WaitForTickersAsync();

            CalculateUSDTBalance();
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
