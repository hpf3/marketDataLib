using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using marketDataLib.DataManagement.Tables;

namespace marketDataLib.DataManagement
{
    public static class LiteDBManager
    {
        public static void StoreSymbol(Symbol symbol)
        {
            using (var db = new LiteDatabase(@"marketData.db"))
            {
                var symbolMeta = db.GetCollection<SymbolMeta>("TrackedSymbols");
                var symbolData = db.GetCollection<SymbolData>(symbol.Meta.SymbolName);
                symbolMeta.Upsert(symbol.Meta);
                symbolData.Upsert(symbol.Data);
            }
        }
        public static void StoreConfiguration(Configuration config)
        {
            using (var db = new LiteDatabase(@"marketData.db"))
            {
                var configuration = db.GetCollection<Configuration>("Configuration");
                configuration.Upsert(config);
            }
        }
        public static Symbol GetSymbol(string symbolName)
        {
            using (var db = new LiteDatabase(@"marketData.db"))
            {
                var symbolMeta = db.GetCollection<SymbolMeta>("TrackedSymbols");
                var symbolData = db.GetCollection<SymbolData>(symbolName);
                return new Symbol(symbolMeta.FindById(symbolName), symbolData.FindAll().ToList());
            }
        }
        public static Configuration GetConfiguration(string key)
        {
            using (var db = new LiteDatabase("marketData.db"))
            {
                var configuration = db.GetCollection<Configuration>("Configuration");
                return configuration.FindById(key);
            }
        }

        //get names of all tracked symbols
        public static List<string> GetTrackedSymbols()
        {
            using (var db = new LiteDatabase("marketData.db"))
            {
                var symbolMeta = db.GetCollection<SymbolMeta>("TrackedSymbols");
                return symbolMeta.FindAll().Select(x => x.SymbolName).ToList();
            }
        }

        //get most recent document for a symbol
        public static SymbolData GetMostRecentSymbolData(string symbolName)
        {
            using (var db = new LiteDatabase(@"marketData.db"))
            {
                var symbolData = db.GetCollection<SymbolData>(symbolName);
                return symbolData.FindOne(Query.All(Query.Descending));
            }
        }

        //get document for a symbol on a specific date if it exists
        public static SymbolData GetSymbolData(string symbolName, DateTime date)
        {
            using (var db = new LiteDatabase("marketData.db"))
            {
                var symbolData = db.GetCollection<SymbolData>(symbolName);
                return symbolData.FindById(date);
            }
        }

        //get all documents for a symbol between two dates
        public static List<SymbolData> GetSymbolData(string symbolName, DateTime startDate, DateTime endDate)
        {
            using (var db = new LiteDatabase("marketData.db"))
            {
                var symbolData = db.GetCollection<SymbolData>(symbolName);
                return symbolData.Find(x => x.Date >= startDate && x.Date <= endDate).ToList();
            }
        }
    }
}