using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace marketDataLib.DataManagement.Tables
{
    public class AvailableSymbol
    {
        public string SymbolName { get; set; }
        public DateTime? Earliest { get; set; }
        [LiteDB.BsonCtor]
        public AvailableSymbol(string symbolName, DateTime? earliest)
        {
            SymbolName = symbolName;
            Earliest = earliest;
        }
    }
}