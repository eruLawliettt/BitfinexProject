﻿using BitfinexConnectorProject.Models;
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
    internal class RestTickersViewModel : ViewModelBase
    {
        private readonly Client _client = new();

        public ObservableCollection<Ticker> TickerData { get; set; } = [];

        public ICommand GetBTCUSDTickerCommand { get; }
        public ICommand GetETHUSDTickerCommand { get; }
        public ICommand GetLTCUSDTickerCommand { get; }
        public ICommand GetETHBTCTickerCommand { get; }
        public ICommand GetLTCBTCTickerCommand { get; }

        public RestTickersViewModel()
        {
            GetBTCUSDTickerCommand = new RelayCommand(async _ => await GetBTCUSDTicker());
            GetETHUSDTickerCommand = new RelayCommand(async _ => await GetETHUSDTicker());
            GetLTCUSDTickerCommand = new RelayCommand(async _ => await GetLTCUSDTicker());
            GetETHBTCTickerCommand = new RelayCommand(async _ => await GetETHBTCDTicker());
            GetLTCBTCTickerCommand = new RelayCommand(async _ => await GetLTCBTCTicker());
        }

        private async Task GetBTCUSDTicker() => await LoadTickerDataAsync("tBTCUSD");
        private async Task GetETHUSDTicker() => await LoadTickerDataAsync("tETHUSD");
        private async Task GetLTCUSDTicker() => await LoadTickerDataAsync("tLTCUSD");
        private async Task GetETHBTCDTicker() => await LoadTickerDataAsync("tETHBTC");
        private async Task GetLTCBTCTicker() => await LoadTickerDataAsync("tLTCBTC");
        

        private async Task LoadTickerDataAsync(string pair)
        {
            if (TickerData.Any())
                TickerData.Clear();

           TickerData.Add(await _client.GetTickerAsync(pair));
        }
    }
}
