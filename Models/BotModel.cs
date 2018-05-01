using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tradeBot.Models
{
    public class BotSettingModel
    {
        public int LastChatID { get; set; }
        public List<string> Receivers { get; set; }
    }
    public class BotResponseModel
    {
        public decimal Pip { get; set; }
        public decimal Sl { get; set; }
        public decimal Tp { get; set; }
        public decimal NasdaqPercent { get; set; }
        public decimal GoldPercent { get; set; }
        public decimal Gold_Price { get; set; }
        public decimal Nasdaq_Open { get; set; }
        public decimal Nasdaq_Close { get; set; }
    }
}
