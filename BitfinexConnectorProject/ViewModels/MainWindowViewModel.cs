using BitfinexConnectorProject.Models;
using BitfinexConnectorProject.Services;
using BitfinexConnectorProject.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace BitfinexConnectorProject.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
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

        public ICommand RestTradesViewCommand { get; }
        public ICommand RestCandlesViewCommand { get; }
        public ICommand RestTickersViewCommand { get; }
        public ICommand WebSocketTradesViewCommand { get; }
        public ICommand WebSocketCandlesViewCommand { get; }

        public MainWindowViewModel()
        {
            RestTradesViewCommand = new RelayCommand(x => ChangeViewToRestTrades());
            RestCandlesViewCommand = new RelayCommand(x => ChangeViewToRestCandles());
            RestTickersViewCommand = new RelayCommand(x => ChangeViewToRestTickers());
            WebSocketTradesViewCommand = new RelayCommand(x => ChangeViewToWebSocketTrades());
            WebSocketCandlesViewCommand = new RelayCommand(x => ChangeViewToWebSocketCandles());
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

    }
}
