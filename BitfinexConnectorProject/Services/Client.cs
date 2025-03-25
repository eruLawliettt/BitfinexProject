using BitfinexConnectorProject.Models;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;

namespace BitfinexConnectorProject.Services
{
    internal class Client : ITestConnector
    {
        #region Rest

        private readonly HttpClient _httpClient;

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            var options = new RestClientOptions($"https://api-pub.bitfinex.com/v2/trades/t{pair}/hist?limit={maxCount}");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            var response = await client.GetAsync(request);

            try
            {
                var tradesDataJsonArray = JsonSerializer.Deserialize<List<List<JsonElement>>>(response.Content);
                List<Trade> trades = [];

                foreach (var trade in tradesDataJsonArray)
                {
                    trades.Add(new Trade
                    {
                        Pair = pair,
                        Price = trade[3].GetDecimal(),
                        Amount = Math.Abs(trade[2].GetDecimal()),
                        Side = trade[2].GetDecimal() > 0 ? "buy" : "sell",
                        Time = DateTimeOffset.FromUnixTimeMilliseconds(trade[1].GetInt64()),
                        Id = trade[0].GetInt64().ToString()
                    });
                }

                return trades;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка парсинга JSON: {ex.Message}");
            }
        }
        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, string period, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            string baseUrl = $"https://api-pub.bitfinex.com/v2/candles/trade%3A{period}%3At{pair}/hist";

            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            if (from.HasValue) queryParams["start"] = from?.ToUnixTimeMilliseconds().ToString();
            if (to.HasValue) queryParams["end"] = to?.ToUnixTimeMilliseconds().ToString();
            if (count != 0) queryParams["limit"] = count.Value.ToString();

            string queryString = queryParams.ToString();
            if (!string.IsNullOrEmpty(queryString)) { baseUrl += $"?{queryString}"; };

            var options = new RestClientOptions(baseUrl);
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            var response = await client.GetAsync(request);

            try
            {
                var candlesDataJsonArray = JsonSerializer.Deserialize<List<List<JsonElement>>>(response.Content);
                List<Candle> candles = [];

                foreach (var candle in candlesDataJsonArray)
                {
                    candles.Add(new Candle
                    {
                        Pair = pair,
                        OpenPrice = candle[1].GetDecimal(),
                        HighPrice = candle[3].GetDecimal(),
                        LowPrice = candle[4].GetDecimal(),
                        ClosePrice = candle[2].GetDecimal(),

                        // значение получается неточное, т.к. тут я просто вычисляю среднюю цену между самой высокой и самой низкой а затем умножаю на объём
                        // по хорошему тут надо получать данные по всем сделкам из апи и считать точное число.
                        TotalPrice = candle[5].GetDecimal() * ((candle[3].GetDecimal() + candle[4].GetDecimal()) / 2),

                        TotalVolume = candle[5].GetDecimal(),
                        OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candle[0].GetInt64())
                    });
                }

                return candles;

            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка парсинга JSON: {ex.Message}");
            }
        }
        public async Task<Ticker> GetTickerAsync(string symbol)
        {
            var options = new RestClientOptions($"https://api-pub.bitfinex.com/v2/ticker/{symbol}");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            var response = await client.GetAsync(request);

            try
            {
                var tickerDataJson = JsonSerializer.Deserialize<List<JsonElement>>(response.Content);

                Ticker ticker = new Ticker {
                    Bid = tickerDataJson[0].GetDecimal(),
                    BidSize = tickerDataJson[1].GetDecimal(),
                    Ask = tickerDataJson[2].GetDecimal(),
                    AskSize = tickerDataJson[3].GetDecimal(),
                    DailyChange = tickerDataJson[4].GetDecimal(),
                    DailyChangeRelative = tickerDataJson[5].GetDecimal(),
                    LastPrice = tickerDataJson[6].GetDecimal(),
                    Volume = tickerDataJson[7].GetDecimal(),
                    HighPrice = tickerDataJson[8].GetDecimal(),
                    LowPrice = tickerDataJson[9].GetDecimal(),
                };

                return ticker;

            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка парсинга JSON: {ex.Message}");
            }
        }


        #endregion

        #region Socket

        private readonly ClientWebSocket _ws = new();
        private readonly Uri _webSocketUri = new("wss://api-pub.bitfinex.com/ws/2");
        private readonly Dictionary<string, int> _subscribsions = new();
        private readonly Dictionary<int, string> _channelMap = new();
        private readonly byte[] _buffer = new byte[8192];

        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;

        public async Task ConnectToWebSocketAsync()
        {
            if (_ws.State == WebSocketState.Open)
                return;

            await _ws.ConnectAsync(_webSocketUri, CancellationToken.None);
            _ = Task.Run(ReceiveMessages); // своего рода костыль, запуск прослушивания, я уверен это можно сделать лучше
        }
        private async Task SendMessage(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        private async Task ReceiveMessages()
        {
            while (_ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _ws.ReceiveAsync(new ArraySegment<byte>(_buffer), CancellationToken.None);
                string message = Encoding.UTF8.GetString(_buffer, 0, result.Count);
                ProcessMessage(message);
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                Console.WriteLine($"Raw JSONL: {message}");
                JsonElement json = default;

                try
                {
                    json = JsonDocument.Parse(message).RootElement;
                }
                catch (Exception ex)
                {
                    // получен битый JSON, пропускаем
                    return;
                }
                 
                if (json.ValueKind == JsonValueKind.Array && json[0].ValueKind == JsonValueKind.Array)
                {
                    // получен снапшот, пропускаем
                    return;
                }

                // если пришел ивент, надо понять какой
                if (json.ValueKind != JsonValueKind.Array && json.TryGetProperty("event", out var eventType))
                {
                    string eventName = eventType.GetString();
                    
                    if (eventName == "subscribed")
                    {
                        string channel = json.GetProperty("channel").GetString();
                        int chanId = json.GetProperty("chanId").GetInt32();

                        if (channel == "trades")
                        {
                            string key = json.GetProperty("symbol").GetString(); // если это трейд, key = symbol
                            _subscribsions[key] = chanId;
                            _channelMap[chanId] = key;
                        }
                        else if (channel == "candles")
                        {
                            string key = json.GetProperty("key").GetString(); // если это свеча, key = key
                            _subscribsions[key] = chanId;
                            _channelMap[chanId] = key;
                        }
                    }
                    else if (eventName == "unsubscribed")
                    {
                        int chanId = json.GetProperty("chanId").GetInt32();
                        if (_channelMap.TryGetValue(chanId, out var key))
                        {
                            _subscribsions.Remove(key);
                            _channelMap.Remove(chanId);
                        }
                    }
                }
                // если после всех проверок массив добрался сюда - значит это либо трейд либо свеча
                else if (json.ValueKind == JsonValueKind.Array)
                {
                    int chanId = json[0].GetInt32();

                    if (_channelMap.TryGetValue(chanId, out var key))
                    {
                        // Trade Update - значит пришел трейд
                        if (json[1].ValueKind == JsonValueKind.String && json[1].GetString() == "tu")
                        {
                            var trade = new Trade
                            {
                                Pair = key, // в данном контексте key -> это пара типа "BTCUSD"
                                Price = json[2][3].GetDecimal(),
                                Amount = Math.Abs(json[2][2].GetDecimal()),
                                Side = json[2][2].GetDecimal() > 0 ? "buy" : "sell",
                                Time = DateTimeOffset.FromUnixTimeMilliseconds(json[2][1].GetInt64()),
                                Id = json[2][0].GetInt64().ToString(),
                            };

                            if (trade.Side == "buy")
                                NewBuyTrade?.Invoke(trade);
                            else
                                NewSellTrade?.Invoke(trade);
                        }
                        // Trade Update нет, зато второй член массива - массив длинной 6, значит пришла свеча
                        else if (json[1].ValueKind == JsonValueKind.Array && json[1].GetArrayLength() == 6)
                        {
                            // скип свечей которые ещё не завершены
                            long currentTimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            if (json[1][0].GetInt64() >= currentTimeStamp - 60_000) // тут хардкод на минутный интервал, позже нужно придумать более гибкую реализацию  
                                return;
                            
                            var candle = new Candle
                            {
                                Pair = key.Split(':')[2], // trade:1m:tBTCUSD
                                OpenPrice = json[1][1].GetDecimal(),
                                HighPrice = json[1][3].GetDecimal(),
                                LowPrice = json[1][4].GetDecimal(),
                                ClosePrice = json[1][2].GetDecimal(),
                                TotalPrice = json[1][5].GetDecimal() * ((json[1][3].GetDecimal() + json[1][4].GetDecimal()) / 2),
                                TotalVolume = json[1][5].GetDecimal(),
                                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(json[1][0].GetInt64())
                            };

                            CandleSeriesProcessing?.Invoke(candle);
                        }
                        // heartbeat ответы игнорируются
                    }
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        
        public async void SubscribeTrades(string pair, int maxCount = 100)
        {
            await ConnectToWebSocketAsync();

            if (_subscribsions.ContainsKey(pair))
                return;

            string message = $"{{\"event\":\"subscribe\", \"channel\":\"trades\", \"symbol\":\"{pair}\"}}";
            await SendMessage(message);
        }
        public async void UnsubscribeTrades(string pair)
        {
            if (!_subscribsions.TryGetValue(pair, out int channelId))
                return;
               
             string message = $"{{\"event\":\"unsubscribe\", \"chanId\":{channelId}}}";
             await SendMessage(message);

            _subscribsions.Remove(pair);
            _channelMap.Remove(channelId);
            
        }

        //-------------------------------------------------------------------------------------------------------------


        public event Action<Candle> CandleSeriesProcessing;
        public async void SubscribeCandles(string pair, string period, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            await ConnectToWebSocketAsync();

            string key = $"trade:{period}:t{pair}";
            string message = $"{{\"event\":\"subscribe\", \"channel\":\"candles\", \"key\":\"{key}\"}}";
            await SendMessage(message);
        }
        public async void UnsubscribeCandles(string pair, string period)
        {
            string key = $"trade:{period}:t{pair}";
            if (!_subscribsions.TryGetValue(key, out int channelId))
                return;

            string message = $"{{\"event\":\"unsubscribe\", \"chanId\":{channelId}}}";
            await SendMessage(message);

            _subscribsions.Remove(key);
            _channelMap.Remove(channelId);
        }



        #endregion
    }
}
