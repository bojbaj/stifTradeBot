using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tradeBot.Models
{      
    public class NasdaqValue
    {
        public decimal Open { get; set; }
        public decimal Close { get; set; }
    }
}
