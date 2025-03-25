using BitfinexConnectorProject.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitfinexConnectorProject.Services
{
    interface ITestConnector
    {
        #region Rest

        Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);

        // int periodInSec был изменён на string period т.к. API bitfinex предоставляет на выбор значения 
        // TimeFrames и не позволяет задавать произвольное кол-во секунд
        Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, string period, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0);

        // метод для получения тикера с биржи
        Task<Ticker> GetTickerAsync(string symbol);

        #endregion



        #region Socket

        event Action<Trade> NewBuyTrade;
        event Action<Trade> NewSellTrade;
        void SubscribeTrades(string pair, int maxCount = 100);
        void UnsubscribeTrades(string pair);

        event Action<Candle> CandleSeriesProcessing;
        //опять же период изменён на string, т.к. Апи не предоставляет возможности выбирать произвольное кол-во секунд
        void SubscribeCandles(string pair, string period, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0);
        void UnsubscribeCandles(string pair, string period);

        #endregion

    }
}
