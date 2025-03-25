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
    internal class WebSocketCandlesViewModel : ViewModelBase
    {
        private readonly Client _client = new();

        private ObservableCollection<Candle> _candles;

        public ObservableCollection<Candle> Candles
        {
            get { return _candles; }
            set => Set(ref _candles, value, nameof(Candles));
        }

        public ICommand SubscribeToBTCUSD1mCommand { get; set; }
        public ICommand UnsubscribeToBTCUSD1mCommand { get; set; }
        public ICommand SubscribeToETHUSD1mCommand { get; set; }
        public ICommand UnsubscribeToETHUSD1mCommand { get; set; }

        public WebSocketCandlesViewModel()
        {
            Candles = [];
            _client.CandleSeriesProcessing += AddCandle;
            SubscribeToBTCUSD1mCommand = new RelayCommand(x => Subscribe("BTCUSD", "1m"));
            UnsubscribeToBTCUSD1mCommand = new RelayCommand(x => Unsubscribe("BTCUSD", "1m"));
            SubscribeToETHUSD1mCommand = new RelayCommand(x => Subscribe("ETHUSD", "1m"));
            UnsubscribeToETHUSD1mCommand = new RelayCommand(x => Unsubscribe("ETHUSD", "1m"));
        }


        private void AddCandle(Candle candle)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Candles.Insert(0, candle);
                if (Candles.Count > 100)
                    Candles.RemoveAt(Candles.Count - 1);
            });
        }

        private void Subscribe(string pair, string period) => _client.SubscribeCandles(pair, period);
        private void Unsubscribe(string pair, string period) => _client.UnsubscribeCandles(pair, period);
    }
}
