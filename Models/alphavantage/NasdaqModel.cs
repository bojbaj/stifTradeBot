using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tradeBot.Models.AlphaVantage
{
    public class MetaData
    {
        [JsonProperty("1. Information")]
        public string Information { get; set; }
        [JsonProperty("2. Symbol")]
        public string Symbol { get; set; }
        [JsonProperty("3. Last Refreshed")]
        public DateTime LastRefreshed { get; set; }
        [JsonProperty("4. Output Size")]
        public string OutputSize { get; set; }
        [JsonProperty("5. Time Zone")]
        public string TimeZone { get; set; }
    }
    public class TimeSeries
    {
        [JsonProperty("1. open")]
        public decimal Open { get; set; }
        [JsonProperty("2. high")]
        public decimal High { get; set; }
        [JsonProperty("3. low")]
        public decimal Low { get; set; }
        [JsonProperty("4. close")]
        public decimal Close { get; set; }
        [JsonProperty("5. volume")]
        public decimal Volume { get; set; }
    }
    public class NasdaqModel
    {
        [JsonProperty("Meta Data")]
        public MetaData MetaData { get; set; }
        [JsonProperty("Time Series (Daily)")]
        public Dictionary<DateTime, TimeSeries> TimeSeries { get; set; }
    }
}
