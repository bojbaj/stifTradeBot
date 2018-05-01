using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using tradeBot.Models;

namespace tradeBot.lib
{
    public enum FetchMode
    {
        yahoo,
        investing,
        tradingview,
        alphavantage,
        forge
    }

    public interface IRestApi
    {
        Task<bool> GetResponse(long chatId);
        Task<bool> Execute();
    }
    public class RestApi : IRestApi
    {
        public readonly IConfigurationRoot _configuration;
        public readonly IHostingEnvironment _env;
        public readonly IBot _bot;
        private readonly string botToken;
        private readonly string fetchMode;
        public RestApi(IConfigurationRoot configuration, IHostingEnvironment env, IBot bot)
        {
            _configuration = configuration;
            _env = env;
            _bot = bot;
            botToken = _configuration.GetValue<string>("Bot:Token");
            fetchMode = _configuration.GetValue<string>("Api:fetchMode");
        }

        public async Task<bool> Execute()
        {
            BotResponseModel jsonResult = await Calculate();
            string strFormattedResult = JsonConvert.SerializeObject(jsonResult, Formatting.Indented);
            string botToken = _configuration.GetValue<string>("Bot:Token");
            Telegram.Bot.TelegramBotClient botClient = new Telegram.Bot.TelegramBotClient(botToken);
            BotSettingModel setting = _bot.LoadSetting();
            foreach (string receiver in setting.Receivers)
            {
                Telegram.Bot.Types.ChatId chatId = new Telegram.Bot.Types.ChatId(receiver);
                await botClient.SendTextMessageAsync(chatId, strFormattedResult);
            }
            return true;
        }

        public async Task<bool> GetResponse(long chatId)
        {
            string botToken = _configuration.GetValue<string>("Bot:Token");
            Telegram.Bot.TelegramBotClient botClient = new Telegram.Bot.TelegramBotClient(botToken);
            try
            {
                BotResponseModel jsonResult = await Calculate();
                string strFormattedResult = JsonConvert.SerializeObject(jsonResult, Formatting.Indented);
                await botClient.SendTextMessageAsync(chatId, strFormattedResult);
                return true;
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, ex.Message);
                return false;
            }
        }

        private async Task<BotResponseModel> Calculate()
        {
            GoldValue responseGold;
            NasdaqValue responseNasdaq;
            FetchMode mode = (FetchMode)Enum.Parse(typeof(FetchMode), fetchMode);
            switch (mode)
            {
                case FetchMode.alphavantage:
                    responseGold = await AlphaVantage_Gold();
                    responseNasdaq = await AlphaVantage_Nasdaq();
                    break;
                case FetchMode.investing:
                    responseGold = await InvestingGold();
                    responseNasdaq = await InvestingNasdaq();
                    break;
                default:
                    return null;
            }

            decimal NasPercent = (responseNasdaq.Open * 100) / responseNasdaq.Close;
            decimal GoldPercent = (NasPercent * 0.4996M) / 100;

            decimal Pip = (responseGold.Price * GoldPercent) / 100;
            decimal Sl = 0;
            decimal A = 0;
            if (responseNasdaq.Open > responseNasdaq.Close)
            {
                A = responseGold.Price + Pip;
                Sl = responseGold.Price - Pip;
            }
            else
            {
                A = responseGold.Price - Pip;
                Sl = responseGold.Price + Pip;
            }
            BotResponseModel jsonResult = new BotResponseModel()
            {
                Pip = Pip,
                Sl = Sl,
                Tp = A,
                NasdaqPercent = NasPercent,
                GoldPercent = GoldPercent,
                Gold_Price = responseGold.Price,
                Nasdaq_Open = responseNasdaq.Open,
                Nasdaq_Close = responseNasdaq.Close
            };
            return jsonResult;
        }

        private void LogResponse(string symbol, string content)
        {
            Int32 timeSpan = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string filePath = _configuration.GetValue<string>("Api:FilePath");
            // string fileFullPath = string.Format("{0}\\{1}\\{2}_{3}.txt",
            //     _env.WebRootPath,
            //     filePath,
            //     timeSpan,
            //     symbol
            //     );
            string dirPath = Path.Combine(
               _env.WebRootPath,
               filePath
               );
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string fileFullPath = Path.Combine(
               dirPath,
               string.Format("{0}_{1}.txt", timeSpan, symbol)
               );

            FileStream logFile = File.Create(fileFullPath);
            StreamWriter logWriter = new StreamWriter(logFile);
            logWriter.WriteLine(DateTime.Now);
            logWriter.WriteLine("============================");
            logWriter.WriteLine(content);
            logWriter.Dispose();
        }

        #region AlphaVantage
        private async Task<NasdaqValue> AlphaVantage_Nasdaq()
        {
            NasdaqValue output = new NasdaqValue();
            try
            {
                string apiUrl = _configuration.GetValue<string>("Api:alphavantage:IXIC:Url");
                string apiKeys = _configuration.GetValue<string>("Api:alphavantage:ApiKeys");
                string apiKey = apiKeys.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).OrderBy(x => Guid.NewGuid()).First();
                apiUrl = string.Format(apiUrl, apiKey);

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string strOutput = await response.Content.ReadAsStringAsync();
                        //LogResponse("NASDAQ", strOutput);
                        Models.AlphaVantage.NasdaqModel model = JsonConvert.DeserializeObject<Models.AlphaVantage.NasdaqModel>(strOutput);
                        output = new NasdaqValue()
                        {
                            Open = model.TimeSeries.FirstOrDefault().Value.Open,
                            Close = model.TimeSeries.FirstOrDefault().Value.Close
                        };
                    }
                }

                return output;
            }
            catch (Exception ex)
            {
                LogResponse("NASDAQ_ERROR", JsonConvert.SerializeObject(ex));
                return null;
            }
        }
        private async Task<GoldValue> AlphaVantage_Gold()
        {
            GoldValue output = new GoldValue();
            try
            {
                string apiUrl = _configuration.GetValue<string>("Api:alphavantage:GOLD:Url");
                string apiKeys = _configuration.GetValue<string>("Api:alphavantage:ApiKeys");
                string apiKey = apiKeys.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).OrderBy(x => Guid.NewGuid()).First();
                apiUrl = string.Format(apiUrl, apiKey);
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string strOutput = await response.Content.ReadAsStringAsync();
                        //LogResponse("GOLD", strOutput);
                        Models.AlphaVantage.GoldModel model = JsonConvert.DeserializeObject<Models.AlphaVantage.GoldModel>(strOutput);
                        output = new GoldValue()
                        {
                            Price = model.RealtimeCurrencyExchangeRate.ExchangeRate
                        };

                    }
                }
                return output;
            }
            catch (Exception ex)
            {
                LogResponse("GOLD_ERROR", JsonConvert.SerializeObject(ex));
                return null;
            }
        }
        #endregion

        #region Investing
        private async Task<NasdaqValue> InvestingNasdaq()
        {
            NasdaqValue output = new NasdaqValue();
            try
            {
                string url = _configuration.GetValue<string>("Api:investing:IXIC:Url");
                string agent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:56.0) Gecko/20100101 Firefox/56.0";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", agent);
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Stream strOutput = await response.Content.ReadAsStreamAsync();
                        HtmlDocument doc = new HtmlDocument();
                        doc.Load(strOutput);
                        HtmlNodeCollection values = doc.DocumentNode.SelectNodes("//span[@class='float_lang_base_2 bold']");
                        HtmlNode value = doc.DocumentNode.SelectSingleNode("//span[@id='last_last']");
                        output = new NasdaqValue()
                        {
                            Open = decimal.Parse(values[3].InnerHtml),
                            Close = decimal.Parse(values[0].InnerHtml)
                        };
                    }
                }

                return output;
            }
            catch (Exception ex)
            {
                LogResponse("NASDAQ_ERROR", JsonConvert.SerializeObject(ex));
                return null;
            }
        }
        private async Task<GoldValue> InvestingGold()
        {
            GoldValue output = new GoldValue();
            try
            {
                string url = _configuration.GetValue<string>("Api:investing:XAUUSD:Url");
                string agent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:56.0) Gecko/20100101 Firefox/56.0";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", agent);
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Stream strOutput = await response.Content.ReadAsStreamAsync();
                        HtmlDocument doc = new HtmlDocument();
                        doc.Load(strOutput);
                        HtmlNode value = doc.GetElementbyId("last_last");
                        output = new GoldValue()
                        {
                            Price = decimal.Parse(value.InnerHtml)
                        };
                    }
                }
                return output;
            }
            catch (Exception ex)
            {
                LogResponse("GOLD_ERROR", JsonConvert.SerializeObject(ex));
                return null;
            }
        }
        #endregion

        //   private async Task<GoldValue> GetGoldPrice_()
        // {
        //     GoldValue output = new GoldValue();
        //     try
        //     {
        //         string symbol = _configuration.GetValue<string>("Api:Symbol");
        //         string baseUrl = _configuration.GetValue<string>("Api:Url");
        //         string interval = _configuration.GetValue<string>("Api:Interval");
        //         Int32 fromTimeSpan = (Int32)(DateTime.Now.AddDays(-2).AddHours(-10).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        //         Int32 toTimeSpan = (Int32)(DateTime.Now.AddDays(-2).AddHours(-9).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

        //         string apiUrl = string.Format("{0}?symbol={1}&resolution={2}&from={3}&to={4}",
        //             baseUrl,
        //             symbol,
        //             interval,
        //             fromTimeSpan,
        //             toTimeSpan
        //             );
        //         string hAuthentication = _configuration.GetValue<string>("Api:Authenticate"); ;
        //         using (HttpClient client = new HttpClient())
        //         {
        //             client.DefaultRequestHeaders.Add("authorization", hAuthentication);
        //             HttpResponseMessage response = await client.GetAsync(apiUrl);

        //             if (response.StatusCode == System.Net.HttpStatusCode.OK)
        //             {
        //                 string strOutput = await response.Content.ReadAsStringAsync();
        //                 LogResponse("GOLD", strOutput);
        //                 GoldModel model = JsonConvert.DeserializeObject<GoldModel>(strOutput);
        //                 output = new GoldValue()
        //                 {
        //                     Price = 1
        //                 };
        //             }
        //         }

        //         return output;
        //     }
        //     catch (Exception ex)
        //     {
        //         LogResponse("GOLD_ERROR", JsonConvert.SerializeObject(ex));
        //         return null;
        //     }
        // }
    }
}
