using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace marketDataLib.DataManagement
{
    public static class Config
    {
        //backing concurrent dictionary
        private static readonly Dictionary<string, string> _config = new();
        //get a value from the config
        public static string Get(string key)
        {
            //if the key exists
            if (_config.TryGetValue(key, out string? value))
            {
                //return the value
                return value;
            }
            //if the key does not exist
            else
            {
                //try to get the value from the database
                var config = LiteDBManager.GetConfiguration(key);
                //add the value to the config
                _config.Add(key, config.Value);
                //return the value
                return config.Value;
            }
        }
        //set a value in the config
        public static void Set(string key, string value)
        {
            _config[key] = value;
            //store the value in the database
            LiteDBManager.StoreConfiguration(new DataManagement.Tables.Configuration { Key = key, Value = value });
        }
    }
}