using BitfinexConnectorProject.Models;
using BitfinexConnectorProject.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BitfinexConnectorProject.ViewModels.WebSocket
{
    internal class WebSocketTradesViewModel : ViewModelBase
    {
        private Client _client;

        private const decimal TradesMaxCount = 100;

        private ObservableCollection<string> _subscribes;
        public ObservableCollection<string> Subscribes
        {
            get { return _subscribes; }
            set => Set(ref _subscribes, value, nameof(Subscribes));
        }

        private ObservableCollection<Trade> _trades;
        public ObservableCollection<Trade> Trades
        {
            get { return _trades; }
            set => Set(ref _trades, value, nameof(Trade));
        }

        public ICommand SubscribeToBTCUSDCommand { get; set; }
        public ICommand UnsubscribeToBTCUSDCommand { get; set; }
        public ICommand SubscribeToETHUSDCommand { get; set; }
        public ICommand UnsubscribeToETHUSDCommand { get; set; }

        public WebSocketTradesViewModel(Client client)
        {
            _client = client;
            _client.NewBuyTrade += AddTrade;
            _client.NewSellTrade += AddTrade;

            Subscribes = [];
            Trades = [];

            SubscribeToBTCUSDCommand = new RelayCommand(x => Subscribe("tBTCUSD"));
            UnsubscribeToBTCUSDCommand = new RelayCommand(x => Unsubscribe("tBTCUSD"));
            SubscribeToETHUSDCommand = new RelayCommand(x => Subscribe("tETHUSD"));
            UnsubscribeToETHUSDCommand = new RelayCommand(x => Unsubscribe("tETHUSD"));
        }

        private void AddTrade(Trade trade)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Trades.Insert(0, trade);
                // если трейдов больше максимально возможного числа, стираются самые старые
                if (Trades.Count > TradesMaxCount)
                    Trades.RemoveAt(Trades.Count - 1);
            });
        }

        private void Subscribe(string pair)
        {
            _client.SubscribeTrades(pair);

            if (!Subscribes.Any(s => s == pair))
                Subscribes.Add(pair);
        }
        private void Unsubscribe(string pair)
        {         
            _client.UnsubscribeTrades(pair);

            if (Subscribes.Any(s => s == pair))
                Subscribes.Remove(pair);
        }

    }
}
