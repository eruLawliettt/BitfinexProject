using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitfinexConnectorProject.Models.DTO
{
    class TickerDTO
    {
        /// <summary>
        /// Валютная пара
        /// </summary>
        public string Pair { get; set; }

        /// <summary>
        /// Bid
        /// </summary>
        public decimal Bid { get; set; }

        /// <summary>
        /// Ask
        /// </summary>
        public decimal Ask { get; set; }

        /// <summary>
        /// Нынешний курс валюты
        /// </summary>
        public decimal BidAskAverage => (Bid + Ask) / 2; 
    }
}
