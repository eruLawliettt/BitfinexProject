using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitfinexConnectorProject.Models
{
    // в задании среди прочего было "Получение информации о тикере (Ticker)", по этой причине я добавил класс для хранения инфы о тикере
    // все свойства совпадают с наполнением JSON из API https://docs.bitfinex.com/reference/rest-public-ticker 
    internal class Ticker
    {
        /// <summary>
        /// Bid
        /// </summary>
        public decimal Bid { get; set; }

        /// <summary>
        /// Сумма 25 наивысших Bid
        /// </summary>
        public decimal BidSize { get; set; }

        /// <summary>
        /// Ask
        /// </summary>
        public decimal Ask { get; set; }

        /// <summary>
        /// Сумма 25 наименьших Ask
        /// </summary>
        public decimal AskSize { get; set; }

        /// <summary>
        /// Сумма, на которую последняя цена изменилась с момента вчерашнего закрытия
        /// </summary>
        public decimal DailyChange { get; set; }

        /// <summary>
        /// Относительное изменение цены с момента вчерашнего закрытия (в процентах)
        /// </summary>
        public decimal DailyChangeRelative { get; set; }

        /// <summary>
        /// Цена последней сделки
        /// </summary>
        public decimal LastPrice { get; set; }

        /// <summary>
        /// Дневной объём торгов
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Наивысшая цена за день
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// Наименьшая цена за день
        /// </summary>
        public decimal LowPrice { get; set; }
    }
}
