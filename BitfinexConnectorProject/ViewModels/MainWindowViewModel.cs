using BitfinexConnectorProject.Services;
using BitfinexConnectorProject.ViewModels.Rest;
using BitfinexConnectorProject.ViewModels.WebSocket;
using BitfinexConnectorProject.Views.Rest;
using BitfinexConnectorProject.Views.WebSocket;
using System.Windows.Input;

namespace BitfinexConnectorProject.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private readonly Client _client = new();

        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set => Set(ref _currentView, value, nameof(CurrentView));
        }

        private RestTradesView _restTradesView = new();
        private RestCandlesView _restCandlesView = new();
        private RestTickersView _restTickersView = new();
        private WebSocketTradesView _webSocketTradesView = new();
        private WebSocketCandlesView _webSocketCandlesView = new();
        private BalanceView _balanceView = new();

        public ICommand RestTradesViewCommand { get; }
        public ICommand RestCandlesViewCommand { get; }
        public ICommand RestTickersViewCommand { get; }
        public ICommand WebSocketTradesViewCommand { get; }
        public ICommand WebSocketCandlesViewCommand { get; }
        public ICommand BalanceViewCommand { get; }

        public MainWindowViewModel()
        {
            ViewsInitialization();

            RestTradesViewCommand = new RelayCommand(x => ChangeViewToRestTrades());
            RestCandlesViewCommand = new RelayCommand(x => ChangeViewToRestCandles());
            RestTickersViewCommand = new RelayCommand(x => ChangeViewToRestTickers());
            WebSocketTradesViewCommand = new RelayCommand(x => ChangeViewToWebSocketTrades());
            WebSocketCandlesViewCommand = new RelayCommand(x => ChangeViewToWebSocketCandles());
            BalanceViewCommand = new RelayCommand(x => ChangeViewToBalance());
        }

        private void ViewsInitialization()
        {
            _restTradesView.DataContext = new RestTradesViewModel(_client);
            _restCandlesView.DataContext = new RestCandlesViewModel(_client);
            _restTickersView.DataContext = new RestTickersViewModel(_client);
            _webSocketTradesView.DataContext = new WebSocketTradesViewModel(_client);
            _webSocketCandlesView.DataContext = new WebSocketCandlesViewModel(_client);
            _balanceView.DataContext = new BalanceViewModel(_client);
        }

        private void ChangeViewToRestTrades()
        {
            CurrentView = _restTradesView;
        }
        private void ChangeViewToRestCandles()
        {
            CurrentView = _restCandlesView;
        }
        private void ChangeViewToRestTickers()
        {
            CurrentView = _restTickersView;
        }
        private void ChangeViewToWebSocketTrades()
        {
            CurrentView = _webSocketTradesView;
        }
        private void ChangeViewToWebSocketCandles()
        {
            CurrentView = _webSocketCandlesView;
        }
        private void ChangeViewToBalance()
        {
            CurrentView = _balanceView;
        }

    }
}
