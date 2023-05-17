using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace marketDataLib.APIs
{
    public interface IWebApi
    {
        /// <summary>
        /// name of the API
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// per minute request limit for the API
        /// </summary>
        public int RequestLimit { get; }
        /// <summary>
        /// requests remaining for the current reset period
        /// </summary>
        public int RequestsRemaining { get; }
        /// <summary>
        /// time until the current reset period ends
        /// </summary>
        public TimeSpan TimeUntilReset { get;}
    }
}