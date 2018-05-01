﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tradeBot.Models.AlphaVantage
{
    public class RealtimeCurrencyExchangeRate
    {
        [JsonProperty("1. From_Currency Code")]
        public string FromCurrencyCode { get; set; }
        [JsonProperty("2. From_Currency Name")]
        public string FromCurrencyName { get; set; }
        [JsonProperty("3. To_Currency Code")]
        public string ToCurrencyCode { get; set; }
        [JsonProperty("4. To_Currency Name")]
        public string ToCurrencyName { get; set; }
        [JsonProperty("5. Exchange Rate")]
        public decimal ExchangeRate { get; set; }
        [JsonProperty("6. Last Refreshed")]
        public DateTime LastRefreshed { get; set; }
        [JsonProperty("7. Time Zone")]
        public string TimeZone { get; set; }
    }
    public class GoldModel
    {
        [JsonProperty("Realtime Currency Exchange Rate")]
        public RealtimeCurrencyExchangeRate RealtimeCurrencyExchangeRate { get; set; }
    }
}
