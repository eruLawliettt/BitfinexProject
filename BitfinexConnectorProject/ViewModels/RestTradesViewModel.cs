using BitfinexConnectorProject.Models;
using BitfinexConnectorProject.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BitfinexConnectorProject.ViewModels
{
    class RestTradesViewModel : ViewModelBase
    {

        private readonly Client _client = new();

        private ObservableCollection<Trade> _trades;

        public ObservableCollection<Trade> Trades
        {
            get { return _trades; }
            set => Set(ref _trades, value, nameof(Trades));
        }

        public string TradePair { get; set; }
        public int TradeCount { get; set; }

        public ICommand Get10LastBTCUSDTradesCommand { get; }
        public ICommand Get10LastETHUSDTradesCommand { get; }
        public ICommand Get50LastBTCUSDTradesCommand { get; }
        public ICommand GetBTCUSDTradesDataCommand { get; }
        public ICommand GetETHUSDTradesDataCommand { get; }

        public RestTradesViewModel()
        {
            Trades = [];
            Get10LastBTCUSDTradesCommand = new RelayCommand(async _ => await Get10LastBTCUSDTrades());
            Get10LastETHUSDTradesCommand = new RelayCommand(async _ => await Get10LastETHUSDTrades());
            Get50LastBTCUSDTradesCommand = new RelayCommand(async _ => await Get50LastBTCUSDTrades());
            GetBTCUSDTradesDataCommand = new RelayCommand(async _ => await GetBTCUSDTradesData());
            GetETHUSDTradesDataCommand = new RelayCommand(async _ => await GetETHUSDTradesData());
        }

        private async Task Get10LastBTCUSDTrades()
        {
            TradePair = "BTCUSD";
            TradeCount = 10;
            await LoadTradesDataAsync(TradePair, TradeCount);
        }

        private async Task Get10LastETHUSDTrades()
        {
            TradePair = "ETHUSD";
            TradeCount = 10;
            await LoadTradesDataAsync(TradePair, TradeCount);
        }

        private async Task Get50LastBTCUSDTrades()
        {
            TradePair = "BTCUSD";
            TradeCount = 50;
            await LoadTradesDataAsync(TradePair, TradeCount);
        }

        private async Task GetBTCUSDTradesData()
        {
            TradePair = "BTCUSD";
            TradeCount = 0;
            await LoadTradesDataAsync(TradePair, TradeCount);
        }

        private async Task GetETHUSDTradesData()
        {
            TradePair = "ETHUSD";
            TradeCount = 0;
            await LoadTradesDataAsync(TradePair, TradeCount);
        }

        private async Task LoadTradesDataAsync(string pair, int count)
        {
            var trades = await _client.GetNewTradesAsync(pair, count);
            Trades.Clear();
            foreach (var trade in trades)
                Trades.Add(trade);
        }
    }
}
