using BitfinexConnectorProject.Models;
using BitfinexConnectorProject.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BitfinexConnectorProject.ViewModels.Rest
{
    class RestTradesViewModel : ViewModelBase
    {
        private Client _client;

        private ObservableCollection<Trade> _trades;
        public ObservableCollection<Trade> Trades
        {
            get { return _trades; }
            set => Set(ref _trades, value, nameof(Trades));
        }

        public ICommand Get10LastBTCUSDTradesCommand { get; }
        public ICommand Get10LastETHUSDTradesCommand { get; }
        public ICommand Get50LastBTCUSDTradesCommand { get; }
        public ICommand GetBTCUSDTradesDataCommand { get; }
        public ICommand GetETHUSDTradesDataCommand { get; }

        public RestTradesViewModel(Client client)
        {
            _client = client;

            Trades = [];

            Get10LastBTCUSDTradesCommand = new RelayCommand(async _ => await Get10LastBTCUSDTrades());
            Get10LastETHUSDTradesCommand = new RelayCommand(async _ => await Get10LastETHUSDTrades());
            Get50LastBTCUSDTradesCommand = new RelayCommand(async _ => await Get50LastBTCUSDTrades());
            GetBTCUSDTradesDataCommand = new RelayCommand(async _ => await GetBTCUSDTradesData());
            GetETHUSDTradesDataCommand = new RelayCommand(async _ => await GetETHUSDTradesData());
        }

        private async Task Get10LastBTCUSDTrades() => await LoadTradesDataAsync("tBTCUSD", 10);
        private async Task Get10LastETHUSDTrades() => await LoadTradesDataAsync("tETHUSD", 10);
        private async Task Get50LastBTCUSDTrades() => await LoadTradesDataAsync("tBTCUSD", 50);
        private async Task GetBTCUSDTradesData() => await LoadTradesDataAsync("tBTCUSD", 0);
        private async Task GetETHUSDTradesData() => await LoadTradesDataAsync("tETHUSD", 0);
        
        private async Task LoadTradesDataAsync(string pair, int count)
        {
            var trades = await _client.GetNewTradesAsync(pair, count);
            Trades.Clear();
            foreach (var trade in trades)
                Trades.Add(trade);
        }
    }
}
