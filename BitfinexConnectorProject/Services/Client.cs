using BitfinexConnectorProject.Models;
using BitfinexConnectorProject.Models.DTO;
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
    // Согласно SOLID, следует разделить интерфейс на IRestTestConnector и ISocketTestConnector, соответственно и класс Client по-хорошему
    // разделить по сферам применения, однако в изначальных файлах задания, весь функционал был описан в одном классе, поэтому я решил 
    // оставить как есть
    internal class Client : ITestConnector
    {
        #region Rest

        private readonly HttpClient _httpClient;

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            var options = new RestClientOptions($"https://api-pub.bitfinex.com/v2/trades/{pair}/hist?limit={maxCount}");
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
        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, string period, 
            DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
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

        private ClientWebSocket _ws = new();
        private readonly Uri _webSocketUri = new("wss://api-pub.bitfinex.com/ws/2");
        private readonly Dictionary<string, int> _subscribsions = new();
        private readonly Dictionary<int, string> _channelMap = new();
        private readonly byte[] _buffer = new byte[8192];

        public bool IsConnected = false;

        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;
        public event Action<Candle> CandleSeriesProcessing;
        public event Action<TickerDTO> TickerDTOProcessing;

        public async Task ConnectToWebSocketAsync()
        {
            if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.Connecting)
                return;

            if (_ws.State == WebSocketState.Closed || _ws.State == WebSocketState.Aborted)
            {
                IsConnected = false;
                _ws.Dispose();
                _ws = new ClientWebSocket();
            }
            
            await _ws.ConnectAsync(_webSocketUri, CancellationToken.None);
            IsConnected = true;
            _ = Task.Run(ReceiveMessages);
            
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
                        // по хорошему лучше заменить на switch, #TODO
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
                        else if (channel == "ticker")
                        {
                            string key = json.GetProperty("symbol").GetString() + "ticker"; // если это тикер, key = symbol + "ticker" 
                            _subscribsions[key] = chanId;                                   // (нужно чтобы можно было отличить тикер от трейда)
                            _channelMap[chanId] = key;
                        };
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
                // если после всех проверок массив добрался сюда - значит это либо трейд либо свеча либо тикер
                else if (json.ValueKind == JsonValueKind.Array)
                {
                    int chanId = json[0].GetInt32();

                    if (_channelMap.TryGetValue(chanId, out var key))
                    {
                        // tu = Trade Update - значит пришел трейд
                        if (json[1].ValueKind == JsonValueKind.String && json[1].GetString() == "tu")
                        {
                            var trade = new Trade
                            {
                                Pair = key, // в данном контексте key -> это пара типа "tBTCUSD"
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
                        // Trade Update нет, зато второй член массива - массив длинной или 6 или 10, если 6 - пришла свеча
                        else if (json[1].ValueKind == JsonValueKind.Array && json[1].GetArrayLength() == 6)
                        {  
                            var candle = new Candle
                            {
                                Pair = key.Split(':')[2],
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
                        // если длинна массива 10 - пришел апдейт тикера
                        else if (json[1].ValueKind == JsonValueKind.Array && json[1].GetArrayLength() == 10)
                        {
                            var tickerDTO = new TickerDTO
                            {
                                Pair = key,
                                Bid = json[1][0].GetDecimal(),
                                Ask = json[1][2].GetDecimal(),
                            };

                            TickerDTOProcessing?.Invoke(tickerDTO);
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

 
        // maxCount я бы тут убрал, т.к. описывать такие ограничения мне удобнее непосредственно во ViewModel которая использует этот метод
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

        public async void SubscribeCandles(string pair, string period, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            await ConnectToWebSocketAsync();

            string key = $"trade:{period}:{pair}";
            if (_subscribsions.TryGetValue(key, out int channelId))
                return;

            string message = $"{{\"event\":\"subscribe\", \"channel\":\"candles\", \"key\":\"{key}\"}}";
            await SendMessage(message);
        }
        public async void UnsubscribeCandles(string pair, string period)
        {
            string key = $"trade:{period}:{pair}";
            if (!_subscribsions.TryGetValue(key, out int channelId))
                return;

            string message = $"{{\"event\":\"unsubscribe\", \"chanId\":{channelId}}}";
            await SendMessage(message);

            _subscribsions.Remove(key);
            _channelMap.Remove(channelId);
        }
        
        public async void SubscribeTickers(string pair)
        {
            await ConnectToWebSocketAsync();
            // + "ticker" является обозначением тикера, если его не будет, пары будут путаться с подписками на трейды
            if (_subscribsions.TryGetValue(pair + "ticker", out int channelId)) 
                return;

            string message = $"{{ \"event\": \"subscribe\", \"channel\": \"ticker\", \"symbol\": \"{pair}\"}}";
            await SendMessage(message);
        }
        public async void UnsubscribeTickers(string pair)
        {
            if (!_subscribsions.TryGetValue(pair + "ticker", out int channelId))
                return;

            string message = $"{{\"event\":\"unsubscribe\", \"chanId\":{channelId}}}";
            await SendMessage(message);

            _subscribsions.Remove(pair + "ticker");
            _channelMap.Remove(channelId);
        }

        #endregion
    }
}
