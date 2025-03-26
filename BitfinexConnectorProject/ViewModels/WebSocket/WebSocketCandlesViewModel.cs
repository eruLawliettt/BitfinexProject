using BitfinexConnectorProject.Models;
using BitfinexConnectorProject.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BitfinexConnectorProject.ViewModels.WebSocket
{
    internal class WebSocketCandlesViewModel : ViewModelBase
    {
        private Client _client;

        private const decimal CandlesMaxCount = 100;

        private ObservableCollection<string> _subscribes;
        public ObservableCollection<string> Subscribes
        {
            get { return _subscribes; }
            set => Set(ref _subscribes, value, nameof(Subscribes));
        }

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

        public WebSocketCandlesViewModel(Client client)
        {
            _client = client;
            _client.CandleSeriesProcessing += AddCandle;

            Subscribes = [];
            Candles = [];
            
            // в данной реализации хард код на обработку подписок с периодом в 1 минуту #TODO переделать
            SubscribeToBTCUSD1mCommand = new RelayCommand(x => Subscribe("tBTCUSD", "1m"));
            UnsubscribeToBTCUSD1mCommand = new RelayCommand(x => Unsubscribe("tBTCUSD", "1m"));
            SubscribeToETHUSD1mCommand = new RelayCommand(x => Subscribe("tETHUSD", "1m"));
            UnsubscribeToETHUSD1mCommand = new RelayCommand(x => Unsubscribe("tETHUSD", "1m"));
        }

        private void AddCandle(Candle candle)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (Candles.Count == 0)
                    Candles.Insert(0, candle);
                // пересчёт незавершенной свечи в момент апдейта - хард код на период в 1 минуту!!! #TODO переделать
                else if (Candles.Any(c => c.Pair == candle.Pair && c.OpenTime.Minute == candle.OpenTime.Minute))
                {
                    Candle badCandle = Candles.First(c => c.Pair == candle.Pair && c.OpenTime.Minute == candle.OpenTime.Minute);
                    Candles.Insert(Candles.IndexOf(badCandle), candle);
                    Candles.Remove(badCandle);
                }
                else
                    Candles.Insert(0, candle);
                // если свечей больше максимально возможного числа, стираются самые старые
                if (Candles.Count > CandlesMaxCount)
                    Candles.RemoveAt(Candles.Count - 1);
            });
        }

        private void Subscribe(string pair, string period)
        { 
            _client.SubscribeCandles(pair, period);

            string key = pair + "p" + period;
            if (!Subscribes.Any(s => s == key))
                Subscribes.Add(key);
        }
        private void Unsubscribe(string pair, string period) 
        { 
            _client.UnsubscribeCandles(pair, period);

            string key = pair + "p" + period;
            if (Subscribes.Any(s => s == key))
                Subscribes.Remove(key);
        }
    }
}
