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
    class RestCandlesViewModel : ViewModelBase
    {
        private readonly Client _client = new();

        private ObservableCollection<Candle> _candles;

        public ObservableCollection<Candle> Candles
        {
            get { return _candles; }
            set => Set(ref _candles, value, nameof(Candles));
        }

        public string TradePair { get; set; }

        private readonly List<string> _candlePeriod = [ "1m", "5m", "15m", "30m", "1h", "3h", "6h", "12h", "1D", "1W", "14D", "1M" ];

        public ICommand GetLastBTCUSDCandlesInPeriod1mCommand { get; }
        public ICommand GetLastETHUSDCandlesInPeriod15mCommand { get; }
        public ICommand Get5LastBTCUSDCandlesInPeriod15mFromToCommand { get; }
        public ICommand GetLastBTCUSDCandlesInPeriod1DCommand { get; }
        public ICommand GetLastETHUSDCandlesInPeriod1DFrom1MarchCommand { get; }

        public RestCandlesViewModel()
        {
            Candles = [];
            GetLastBTCUSDCandlesInPeriod1mCommand = new RelayCommand(async _ => await GetLastBTCUSDCandlesInPeriod1m());
            GetLastETHUSDCandlesInPeriod15mCommand = new RelayCommand(async _ => await GetLastETHUSDCandlesInPeriod15m());
            Get5LastBTCUSDCandlesInPeriod15mFromToCommand = new RelayCommand(async _ => await Get5LastBTCUSDCandlesInPeriod15mFromTo());
            GetLastBTCUSDCandlesInPeriod1DCommand = new RelayCommand(async _ => await GetLastBTCUSDCandlesInPeriod1D());
            GetLastETHUSDCandlesInPeriod1DFrom1MarchCommand = new RelayCommand(async _ => await GetLastETHUSDCandlesInPeriod1DFrom1March());
        }

        private async Task GetLastBTCUSDCandlesInPeriod1m()
        {
            await LoadCandlesDataAsync("BTCUSD", _candlePeriod[0], null);
        }
        private async Task GetLastETHUSDCandlesInPeriod15m()
        {
            await LoadCandlesDataAsync("ETHUSD", _candlePeriod[2], null);
        }
        private async Task Get5LastBTCUSDCandlesInPeriod15mFromTo()
        {
            await LoadCandlesDataAsync("BTCUSD", _candlePeriod[2], 
                new DateTimeOffset(2025, 03, 23, 6, 7, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 03, 23, 9, 2, 0, TimeSpan.Zero), 5);
        }
        private async Task GetLastBTCUSDCandlesInPeriod1D()
        {
            await LoadCandlesDataAsync("BTCUSD", _candlePeriod[8], null);
        }
        private async Task GetLastETHUSDCandlesInPeriod1DFrom1March()
        {
            await LoadCandlesDataAsync("ETHUSD", _candlePeriod[8], new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero));
        }




        private async Task LoadCandlesDataAsync(string pair, string period, DateTimeOffset? from, DateTimeOffset? to = null, int? count = 0)
        {
            var candles = await _client.GetCandleSeriesAsync(pair, period, from, to, count);
            Candles.Clear();
            foreach (var candle in candles)
                Candles.Add(candle);
        }

    }
}
