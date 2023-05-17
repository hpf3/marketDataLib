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
        public static Uri DatabasePath { get; set; } = new Uri("marketData.db", UriKind.Relative);
        public static void StoreSymbol(Symbol symbol,string api)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var symbolMeta = db.GetCollection<SymbolMeta>("Tracked_"+api);
                var symbolData = db.GetCollection<SymbolData>(symbol.Meta.SymbolName+"_"+api);
                symbolMeta.Upsert(symbol.Meta);
                symbolData.Upsert(symbol.Data);
            }
        }
        public static void StoreSymbol(List<Symbol> symbols,string api)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var symbolMeta = db.GetCollection<SymbolMeta>("Tracked_"+api);
                foreach (var symbol in symbols)
                {
                    var symbolData = db.GetCollection<SymbolData>(symbol.Meta.SymbolName+"_"+api);
                    symbolMeta.Upsert(symbol.Meta);
                    symbolData.Upsert(symbol.Data);
                }
            }
        }
        public static void StoreConfiguration(Configuration config)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var configuration = db.GetCollection<Configuration>("Configuration");
                configuration.Upsert(config);
            }
        }
        public static Symbol GetSymbol(string symbolName,string api)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var symbolMeta = db.GetCollection<SymbolMeta>("Tracked_"+api);
                var symbolData = db.GetCollection<SymbolData>(symbolName+"_"+api);
                return new Symbol(symbolMeta.FindById(symbolName), symbolData.FindAll().ToList());
            }
        }
        public static Configuration GetConfiguration(string key)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var configuration = db.GetCollection<Configuration>("Configuration");
                Configuration? result = configuration.FindById(key);
                //if the result is null
                if (result == null)
                {
                    //return a new configuration with the key and empty value
                    return new Configuration { Key = key, Value = "" };
                }
                //if the result is not null
                else
                {
                    //return the result
                    return result.Value;
                }
            }
        }

        //get names of all tracked symbols
        public static List<string> GetTrackedSymbols(string api)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var symbolMeta = db.GetCollection<SymbolMeta>("Tracked_"+api);
                return symbolMeta.FindAll().Select(x => x.SymbolName).ToList();
            }
        }

        //get most recent document for a symbol
        public static SymbolData GetMostRecentSymbolData(string symbolName, string api)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var symbolData = db.GetCollection<SymbolData>(symbolName+"_"+api);
                return symbolData.FindOne(Query.All(Query.Descending));
            }
        }

        //get document for a symbol on a specific date if it exists
        public static SymbolData GetSymbolData(string symbolName, DateTime date, string api)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var symbolData = db.GetCollection<SymbolData>(symbolName+"_"+api);
                return symbolData.FindById(date);
            }
        }

        //get all documents for a symbol between two dates
        public static List<SymbolData> GetSymbolData(string symbolName, DateTime startDate, DateTime endDate, string api)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var symbolData = db.GetCollection<SymbolData>(symbolName+"_"+api);
                return symbolData.Find(x => x.Date >= startDate && x.Date <= endDate).ToList();
            }
        }

        //get symbol metadata
        public static SymbolMeta GetSymbolMeta(string symbolName, string api)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var symbolMeta = db.GetCollection<SymbolMeta>("Tracked_"+api);
                return symbolMeta.FindById(symbolName);
            }
        }

        //request log for credit usage tracking
        public static void LogRequest(this APIRequestRecord record, string ApiName)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var requestLog = db.GetCollection<APIRequestRecord>("Log_"+ApiName);
                requestLog.Insert(record);
            }
        }

        //cache for the available symbols for each api
        public static void CacheAvailableSymbols(List<string> symbols, string ApiName)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var availableSymbols = db.GetCollection<AvailableSymbol>("AvailableSymbols_"+ApiName);
                availableSymbols.DeleteAll();
                availableSymbols.InsertBulk(symbols.Select(x => new AvailableSymbol(x, null)));
            }
        }

        //get the cached available symbols for an api
        public static List<AvailableSymbol> GetAvailableSymbols(string ApiName)
        {
            using (var db = new LiteDatabase(DatabasePath.AbsolutePath))
            {
                var availableSymbols = db.GetCollection<AvailableSymbol>("AvailableSymbols_"+ApiName);
                return availableSymbols.FindAll().ToList();
            }
        }
    }
    public struct APIRequestRecord{
        public DateTime RequestTime {get;set;}
        public string url {get;set;}
        public int cost {get;set;}
        public string ApiName {get;set;}
        public APIRequestRecord(string url, int cost, string ApiName){
            RequestTime = DateTime.Now;
            this.url = url;
            this.cost = cost;
            this.ApiName = ApiName;
        }
    }
}