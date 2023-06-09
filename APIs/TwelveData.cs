using System.Security.AccessControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using marketDataLib.DataManagement;

namespace marketDataLib.APIs;
public class TwelveData : IWebApi
{
    private TwelveDataConfig _config;
    public const string ApiEndpoint = "https://api.twelvedata.com";
    public string Name => "TwelveData_"+_config.Interval;
    private int _todayRequests = 0;
    private int MinuteRequests => _requests.Sum(x => x.Cost);
    public int RequestLimit => _config.CreditLimitTier.DailyLimit - Interlocked.Add(ref _todayRequests, 0);
    public int RequestsRemaining => _config.CreditLimitTier.MinuteLimit - MinuteRequests;
    public TimeSpan TimeUntilReset => _config.CreditLimitTier.TimeUntilReset();
    //threadsafe buffer and timer to manage the per minute request count and logging of requests
    private readonly System.Collections.Concurrent.ConcurrentBag<APIRequestRecord> _requests = new();
    #pragma warning disable IDE0052 // Remove unread private members
    private readonly System.Threading.Timer _timer;
    private readonly System.Timers.Timer _DailyTimer;
    #pragma warning restore IDE0052 // Remove unread private members

    public TwelveData(TwelveDataConfig config)
    {
        _config = config;
        _timer = new Timer(ResetMinuteRequests, null, 0, 60000);
        _DailyTimer = new System.Timers.Timer(config.CreditLimitTier.TimeUntilReset());
        _DailyTimer.Elapsed += ResetDailyRequests;
    }

    private void ResetDailyRequests(object? sender, System.Timers.ElapsedEventArgs e)
    {
        //threadsafe reset of the per day request count
        System.Threading.Interlocked.Exchange(ref _todayRequests, 0);

        _DailyTimer.Interval = _config.CreditLimitTier.TimeUntilReset().TotalMilliseconds;
    }

    private void ResetMinuteRequests(object? state)
    {
        //log the requests individually
        while (_requests.TryTake(out var request))
        {
            //increment the number of requests made today using thread safe method
            System.Threading.Interlocked.Add(ref _todayRequests, request.Cost);
            //log the request
            request.LogRequest(Name);
        }
    }

    /// <summary>
    /// Gets the earliest date that the API can provide data for a given symbol
    /// </summary>
    /// <param name="symbol">the symbol to check</param>
    /// <returns>the earliest date that the API can provide data for a given symbol</returns>
    /// <exception cref="System.ArgumentException">Thrown when the symbol is not found</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the API returns an error</exception>
    public async Task<DateTime> GetEarliestDate(string symbol)
    {
        var available = await GetAvailableSymbols();
        //check if the symbol is available
        if (!available.Any(x => x.SymbolName == symbol))
        {
            //throw an exception
            throw new ArgumentException("Symbol not found");
        }
        //check if the earliest date was cached
        if (available.First(x => x.SymbolName == symbol).Earliest is DateTime date)
        {
            //return the cached date
            return date;
        }
        //if the earliest date was not cached, make a request to the API
        const string MethodUrl = "/earliest_timestamp";
        string url = ApiEndpoint+ MethodUrl + $"?symbol={symbol}&interval={_config.Interval.IntervalToString()}&apikey={_config.ApiKey}";
        var response = await new System.Net.Http.HttpClient().GetAsync(url);
        _requests.Add(new DataManagement.APIRequestRecord(url, 1, Name));
        //check if the response returned
        if (response.IsSuccessStatusCode)
        {
            //convert the response body to TwelveDataEarliestDateResponse
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<TwelveDataEarliestDateResponse>(await response.Content.ReadAsStringAsync());
            return data.DateTime;
        }
        else
        {
            //throw an exception
            throw new System.Net.Http.HttpRequestException("Request failed");
        }
    }

    /// <summary>
    /// gets the available symbols for the API
    /// </summary>
    public async Task<List<DataManagement.Tables.AvailableSymbol>> GetAvailableSymbols()
    {
        //check if the symbols were updated today
        if (DataManagement.Config.Get("TwelveDataLastUpdated") == DateTime.Now.ToString("yyyy-MM-dd"))
        {
            //return the symbols from the database
            return DataManagement.LiteDBManager.GetAvailableSymbols(Name);
        }
        //if the symbols were not updated today, make a request to the API
        const string MethodUrl = "/stocks";
        const string url = ApiEndpoint + MethodUrl;
        var response = await new System.Net.Http.HttpClient().GetAsync(url);

        //check if the response returned
        if (response.IsSuccessStatusCode)
        {
            //convert the response body to TwelveDataSymbolResponse
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<TwelveDataAvailableSymbolsResponse>(await response.Content.ReadAsStringAsync());

            //store in the main config the time that the symbols were last updated
            DataManagement.Config.Set("TwelveDataLastUpdated", DateTime.Now.ToString("yyyy-MM-dd"));

            //filter the symbols to only include stocks that are available with the current plan
            var filtered = data.Data.Where(x => x.Access.Plan == _config.CreditLimitTier.PlanName).Select(x => x.Symbol).ToList();

            //add the symbols to the database
            DataManagement.LiteDBManager.CacheAvailableSymbols(filtered, Name);

            //return the symbols
            return filtered.ConvertAll(x => new DataManagement.Tables.AvailableSymbol(x, null));
        }
        else
        {
            //throw an exception
            throw new System.Net.Http.HttpRequestException("Request failed");
        }
    }
    /// <summary>
    /// Gets TimeSeries data for a given symbol
    /// </summary>
    /// <param name="symbol">the symbol to get data for</param>
    /// <param name="interval">the interval to get data for</param>
    /// <param name="start">the start date to get data for</param>
    /// <param name="end">the end date to get data for</param>
    /// <returns>the TimeSeries data for a given symbol</returns>
    /// <exception cref="System.ArgumentException">Thrown when the symbol is not found</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the API returns an error</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the start date is before the earliest date available</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the end date is after the latest date available</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the start date is after the end date</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the start date is after the current date</exception>
    public async Task<DataManagement.Tables.Symbol> GetTimeSeries(string symbol, DateTime start, DateTime end)
    {
        //check if the start date is after the current date
        if (start > DateTime.Now)
        {
            //throw an exception
            throw new ArgumentOutOfRangeException(nameof(start), "start cannot be after current date");
        }
        //check if the start date is after the end date
        if (start > end)
        {
            //throw an exception
            throw new ArgumentOutOfRangeException(nameof(start), $"start cannot be after end{end}");
        }
        //check if the start date is before the earliest date available
        if (start < await GetEarliestDate(symbol))
        {
            //throw an exception
            throw new ArgumentOutOfRangeException(nameof(start),"start cannot be before earliest date available");
        }
        //check if the end date is after the latest date available
        if (end > DateTime.Now)
        {
            //throw an exception
            throw new ArgumentOutOfRangeException(nameof(end),"end cannot be after current date");
        }
        //check if the end date is after the latest date available
        if (end > DateTime.Today.AddDays(-1))
        {
            //throw an exception
            throw new ArgumentOutOfRangeException(nameof(end),"end cannot be after latest date available");
        }

        //check if the data is cached
        if (DataManagement.LiteDBManager.GetTrackedSymbols(Name).Any(x => x == symbol))
        {
            var symbolData= DataManagement.LiteDBManager.GetSymbolData(symbol, start, end, Name);
            var symbolMeta = DataManagement.LiteDBManager.GetSymbolMeta(symbol, Name);
            return new DataManagement.Tables.Symbol(symbolMeta, symbolData);
        }

        //if the data is not cached, make a request to the API
        const string MethodUrl = "/time_series";
        string url = ApiEndpoint + MethodUrl + $"?symbol={symbol}&interval={_config.Interval.IntervalToString()}&start_date="+start.ToString("yyyy-MM-dd")+$"&end_date={end:yyyy-MM-dd}&apikey={_config.ApiKey}";
        var response = await new System.Net.Http.HttpClient().GetAsync(url);
        _requests.Add(new DataManagement.APIRequestRecord(url, 1, Name));
        //check if the response returned
        if (response.IsSuccessStatusCode)
        {
            //convert the response body to TwelveDataTimeSeriesResponse
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<TwelveDataTimeSeriesResponse>(await response.Content.ReadAsStringAsync()).ToSymbol();

            //add the data to the database
            DataManagement.LiteDBManager.StoreSymbol(data, Name);

            //return the data
            return data;
        }
        else
        {
            //throw an exception
            throw new System.Net.Http.HttpRequestException("Request failed");
        }
    }

    /// <summary>
    /// Gets the available indicators
}

public struct TwelveCreditLimitTier
{
    //number of requests allowed per day
    public int DailyLimit;
    //number of requests allowed per minute
    public int MinuteLimit;

    //time after beginning of day when the reset period starts
    public TimeSpan ResetTime;

    //the plan name for the tier
    public string PlanName;
    public TwelveCreditLimitTier(int dailyLimit, int minuteLimit, TimeSpan resetTime,string PlanName)
    {
        DailyLimit = dailyLimit;
        MinuteLimit = minuteLimit;
        ResetTime = resetTime;
        this.PlanName = PlanName;
    }
    public readonly TimeSpan TimeUntilReset()
    {
        DateTime now = DateTime.Now;
        DateTime reset = new(now.Year, now.Month, now.Day, ResetTime.Hours, ResetTime.Minutes, ResetTime.Seconds);
        if (now > reset)
        {
            reset.AddDays(1);
        }
        return reset - now;
    }

    //predefined tiers
    public static readonly TwelveCreditLimitTier Free = new(800, 8, new TimeSpan(0, 0, 0),"Basic");
}
public struct TwelveDataConfig
{
    public string ApiKey;
    public TwelveDataInterval Interval;
    public TwelveCreditLimitTier CreditLimitTier;
    public TwelveDataConfig(string apiKey, TwelveCreditLimitTier creditLimitTier)
    {
        ApiKey = apiKey;
        CreditLimitTier = creditLimitTier;
    }
}
public enum TwelveDataInterval
{
    OneMin,
    FiveMin,
    FifteenMin,
    ThirtyMin,
    FourtyFiveMin,
    OneHour,
    OneDay,
    OneWeek,
    OneMonth,
}

public static class TwelveDataIntervalConverter
{
    //converts the enum to the string used by the API
    public static string IntervalToString(this TwelveDataInterval interval)
    {
        return interval switch
        {
            TwelveDataInterval.OneMin => "1min",
            TwelveDataInterval.FiveMin => "5min",
            TwelveDataInterval.FifteenMin => "15min",
            TwelveDataInterval.ThirtyMin => "30min",
            TwelveDataInterval.FourtyFiveMin => "45min",
            TwelveDataInterval.OneHour => "1hour",
            TwelveDataInterval.OneDay => "1day",
            TwelveDataInterval.OneWeek => "1week",
            TwelveDataInterval.OneMonth => "1month",
            _ => throw new ArgumentException("Invalid interval"),
        };
    }

    //converts the string used by the API to the enum
    public static TwelveDataInterval StringToInterval(this string interval)
    {
        return interval switch
        {
            "1min" => TwelveDataInterval.OneMin,
            "5min" => TwelveDataInterval.FiveMin,
            "15min" => TwelveDataInterval.FifteenMin,
            "30min" => TwelveDataInterval.ThirtyMin,
            "45min" => TwelveDataInterval.FourtyFiveMin,
            "1hour" => TwelveDataInterval.OneHour,
            "1day" => TwelveDataInterval.OneDay,
            "1week" => TwelveDataInterval.OneWeek,
            "1month" => TwelveDataInterval.OneMonth,
            _ => throw new ArgumentException("Invalid interval"),
        };
    }

    //converts the enum to a TimeSpan
    public static TimeSpan IntervalToTimeSpan(this TwelveDataInterval interval)
    {
        return interval switch
        {
            TwelveDataInterval.OneMin => new TimeSpan(0, 1, 0),
            TwelveDataInterval.FiveMin => new TimeSpan(0, 5, 0),
            TwelveDataInterval.FifteenMin => new TimeSpan(0, 15, 0),
            TwelveDataInterval.ThirtyMin => new TimeSpan(0, 30, 0),
            TwelveDataInterval.FourtyFiveMin => new TimeSpan(0, 45, 0),
            TwelveDataInterval.OneHour => new TimeSpan(1, 0, 0),
            TwelveDataInterval.OneDay => new TimeSpan(1, 0, 0, 0),
            TwelveDataInterval.OneWeek => new TimeSpan(7, 0, 0, 0),
            TwelveDataInterval.OneMonth => new TimeSpan(30, 0, 0, 0),
            _ => throw new ArgumentException("Invalid interval"),
        };
    }
    //converts the TimeSpan to the enum
    public static TwelveDataInterval TimeSpanToInterval(this TimeSpan interval)
    {
        if (interval == new TimeSpan(0, 1, 0))
        {
            return TwelveDataInterval.OneMin;
        }
        else if (interval == new TimeSpan(0, 5, 0))
        {
            return TwelveDataInterval.FiveMin;
        }
        else if (interval == new TimeSpan(0, 15, 0))
        {
            return TwelveDataInterval.FifteenMin;
        }
        else if (interval == new TimeSpan(0, 30, 0))
        {
            return TwelveDataInterval.ThirtyMin;
        }
        else if (interval == new TimeSpan(0, 45, 0))
        {
            return TwelveDataInterval.FourtyFiveMin;
        }
        else if (interval == new TimeSpan(1, 0, 0))
        {
            return TwelveDataInterval.OneHour;
        }
        else if (interval == new TimeSpan(1, 0, 0, 0))
        {
            return TwelveDataInterval.OneDay;
        }
        else if (interval == new TimeSpan(7, 0, 0, 0))
        {
            return TwelveDataInterval.OneWeek;
        }
        else if (interval == new TimeSpan(30, 0, 0, 0))
        {
            return TwelveDataInterval.OneMonth;
        }
        else
        {
            throw new ArgumentException("Invalid interval");
        }
    }

    //converts string to TimeSpan
    public static TimeSpan StringToTimeSpan(this string interval)
    {
        return interval switch
        {
            "1min" => new TimeSpan(0, 1, 0),
            "5min" => new TimeSpan(0, 5, 0),
            "15min" => new TimeSpan(0, 15, 0),
            "30min" => new TimeSpan(0, 30, 0),
            "45min" => new TimeSpan(0, 45, 0),
            "1hour" => new TimeSpan(1, 0, 0),
            "1day" => new TimeSpan(1, 0, 0, 0),
            "1week" => new TimeSpan(7, 0, 0, 0),
            "1month" => new TimeSpan(30, 0, 0, 0),
            _ => throw new ArgumentException("Invalid interval"),
        };
    }
}
