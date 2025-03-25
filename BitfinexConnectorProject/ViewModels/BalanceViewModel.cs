﻿using BitfinexConnectorProject.Models;
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

        private ObservableCollection<TickerDTO> _tickers;

        public ObservableCollection<TickerDTO> Tickers
        {
            get { return _tickers; }
            set => Set(ref _tickers, value, nameof(Tickers));
        }

        public BalanceViewModel()
        {
            _client.TickerDTOProcessing += AddTickerDTO;
            Tickers = [];
            _ = InitializeSubscriptionsAsync();

        }

        ~BalanceViewModel() 
        {
        }

        //USDT

        // 1BTC
        // 15_000 XRP
        // 50 XMR
        // 30 DASH

        // Need tickers : tBTCUST, tXRPUST, tXMRUST, tDSHUST(does not exist) => tDSHUSD

        // Current price =  ( BID + ASK ) / 2
        // Get ticker tBTCUST -> calculate -> get usdt value -> decimal UsdtBalance = value
        // Get ticker tXRPUST -> calculate -> get usdt value -> UsdtBalance += value
        // Get ticker tXMRUST -> calculate -> get usdt value -> UsdtBalance += value
        // Get ticker tDSHUST -> calculate -> get usd value -> get ticker tUSDUST -> get usdt value -> UsdtBalance += value
        //
        // USDT Balance complete

        // needed tickers for usdt balance : tBTCUST, tXRPUST, tXMRUST, tDSHUSD, tUSTUSD

        private async Task InitializeSubscriptionsAsync()
        {
            await SubscribeAsync("tBTCUST");
            await SubscribeAsync("tXRPUST");
            await SubscribeAsync("tXMRUST");
            await SubscribeAsync("tDSHUSD");
            await SubscribeAsync("tUSTUSD");
        }

        private void AddTickerDTO(TickerDTO tickerDTO)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Tickers.Add(tickerDTO);   
            });
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
