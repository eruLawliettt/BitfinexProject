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
    internal class WebSocketTradesViewModel : ViewModelBase
    {
		private readonly Client _client = new();

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

        public WebSocketTradesViewModel()
        {
			Trades = [];
			_client.NewBuyTrade += AddTrade;
			_client.NewSellTrade += AddTrade;
			SubscribeToBTCUSDCommand = new RelayCommand(x => Subscribe("tBTCUSD"));
			UnsubscribeToBTCUSDCommand = new RelayCommand(x => Unsubscribe("tBTCUSD"));
            SubscribeToETHUSDCommand = new RelayCommand(x => Subscribe("tETHUSD"));
            UnsubscribeToETHUSDCommand = new RelayCommand(x => Unsubscribe("tETHUSD"));
        }


        private void AddTrade(Trade trade)
		{
			App.Current.Dispatcher.Invoke(() =>
			{
				Trades.Insert(0, trade);
				if (Trades.Count > 100)
					Trades.RemoveAt(Trades.Count - 1);
			});
		}

		private void Subscribe(string pair) => _client.SubscribeTrades(pair);
		private void Unsubscribe(string pair) => _client.UnsubscribeTrades(pair);

		

	}
}
