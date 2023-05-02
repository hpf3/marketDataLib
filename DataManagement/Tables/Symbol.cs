using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
namespace marketDataLib.DataManagement.Tables
{
    public class SymbolMeta
    {
        [BsonId]
        public string SymbolName { get; set; }
        public TimeSpan Interval { get; set; }
        public string Currency { get; set; }
        public string Exchange { get; set; }
        public string Mic_code { get; set; }
        public string Type { get; set; }

        [BsonCtor]
        public SymbolMeta(string symbolName, TimeSpan interval, string currency, string exchange, string mic_code, string type)
        {
            SymbolName = symbolName;
            Interval = interval;
            Currency = currency;
            Exchange = exchange;
            Mic_code = mic_code;
            Type = type;
        }
    }

    // Define a class to represent the symbol data
    public class SymbolData
    {
        [BsonId]
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public int Volume { get; set; }

        [BsonCtor]
        public SymbolData(DateTime date, decimal open, decimal high, decimal low, decimal close, int volume)
        {
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
    }
    /// <summary>
    /// not to be stored directly in the database
    /// </summary>
    public class Symbol{
        public SymbolMeta Meta{get;set;}
        public List<SymbolData> Data{get;set;}
        public Symbol(SymbolMeta meta, List<SymbolData> data){
            Meta=meta;
            Data=data;
        }
    }
}