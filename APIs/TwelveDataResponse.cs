using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// spell-checker:disable
namespace marketDataLib.APIs;

/*TwelveData response for earliest date
example json:
{
  "datetime": "1980-12-12",
  "unix_time": 345427200
}
*/
public struct TwelveDataEarliestDateResponse
{
  [JsonProperty("datetime")]
    public DateTime DateTime { get; set; }
  [JsonProperty("unix_time")]
    public long UnixTime { get; set; }
}

/*TwelveData response for time series
example json:
{
  "meta": {
    "symbol": "AAPL",
    "interval": "1min",
    "currency": "USD",
    "exchange_timezone": "America/New_York",
    "exchange": "NASDAQ",
    "mic_code": "XNAS",
    "type": "Common Stock"
  },
  "values": [
    {
      "datetime": "2021-09-16 15:59:00",
      "open": "148.73500",
      "high": "148.86000",
      "low": "148.73000",
      "close": "148.85001",
      "volume": "624277"
    },
    {
      "datetime": "2021-09-16 15:58:00",
      "open": "148.72000",
      "high": "148.78000",
      "low": "148.70000",
      "close": "148.74001",
      "volume": "274622"
    }
  ],
  "status": "ok"
}
*/
public struct TwelveDataTimeSeriesResponse
{
  [JsonProperty("meta")]
    public TwelveDataMeta Meta { get; set; }
  [JsonProperty("values")]
    public List<TwelveDataValues> Values { get; set; }
  [JsonProperty("status")]
    public string Status { get; set; }

    //converts the TwelveDataTimeSeriesResponse to a Symbol
    public readonly DataManagement.Tables.Symbol ToSymbol()
    {
        return new DataManagement.Tables.Symbol(Meta.ToSymbolMeta(), Values.ConvertAll(v => v.ToSymbolData()));
    }
}
public struct TwelveDataMeta
{
  [JsonProperty("symbol")]
    public string Symbol { get; set; }
  [JsonProperty("interval")]
    public string Interval { get; set; }
  [JsonProperty("currency")]
    public string Currency { get; set; }
  [JsonProperty("exchange_timezone")]
    public string Exchange_timezone { get; set; }
  [JsonProperty("exchange")]
    public string Exchange { get; set; }
  [JsonProperty("mic_code")]
    public string Mic_code { get; set; }
  [JsonProperty("type")]
    public string Type { get; set; }

    //converts the TwelveDataMeta to a SymbolMeta
    public readonly DataManagement.Tables.SymbolMeta ToSymbolMeta()
    {
        return new DataManagement.Tables.SymbolMeta(Symbol, Interval.StringToTimeSpan(), Currency, Exchange, Mic_code, Type);
    }
}
public struct TwelveDataValues
{
  [JsonProperty("datetime")]
    public DateTime DateTime { get; set; }
  [JsonProperty("open")]
    public decimal Open { get; set; }
  [JsonProperty("high")]
    public decimal High { get; set; }
  [JsonProperty("low")]
    public decimal Low { get; set; }
  [JsonProperty("close")]
    public decimal Close { get; set; }
  [JsonProperty("volume")]
    public int Volume { get; set; }

    //converts the TwelveDataValues to a SymbolData
    public readonly DataManagement.Tables.SymbolData ToSymbolData()
    {
        return new DataManagement.Tables.SymbolData(DateTime, Open, High, Low, Close, Volume);
    }
}

/*TwelveData response for available symbols
example json:
{
  "data": [
    {
      "symbol": "TCS",
      "name": "Tata Consultancy Services Limited",
      "currency": "INR",
      "exchange": "NSE",
      "mic_code": "XNSE",
      "country": "India",
      "type": "Common Stock",
      "access": {
        "global": "Level A",
        "plan": "Grow"
      }
    },
    {
      "symbol": "TCS",
      "name": "Axon Enterprise Inc",
      "currency": "EUR",
      "exchange": "FSX",
      "mic_code": "XFRA",
      "country": "Germany",
      "type": "Common Stock",
      "access": {
        "global": "Level A",
        "plan": "Grow"
      }
    },
    {...}
  ],
  "status": "ok"
}
*/
public struct TwelveDataAvailableSymbolsResponse
{
  [JsonProperty("data")]
    public List<TwelveDataAvailableSymbolsData> Data { get; set; }
  [JsonProperty("status")]
    public string Status { get; set; }
}
public struct TwelveDataAvailableSymbolsData
{
  [JsonProperty("symbol")]
    public string Symbol { get; set; }
  [JsonProperty("name")]
    public string Name { get; set; }
  [JsonProperty("currency")]
    public string Currency { get; set; }
  [JsonProperty("exchange")]
    public string Exchange { get; set; }
  [JsonProperty("mic_code")]
    public string Mic_code { get; set; }
  [JsonProperty("country")]
    public string Country { get; set; }
  [JsonProperty("type")]
    public string Type { get; set; }
  [JsonProperty("access")]
    public TwelveDataAvailableSymbolsAccess Access { get; set; }
}
public struct TwelveDataAvailableSymbolsAccess
{
  [JsonProperty("global")]
    public string Global { get; set; }
  [JsonProperty("plan")]
    public string Plan { get; set; }
}

public class IndicatorResponse
{
    public List<Indicator>? Indicators { get; set; }
}

public class Indicator
{
    public bool Enable { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public bool Overlay { get; set; }
    public Dictionary<string, Parameter>? Parameters { get; set; }
    public Dictionary<string, OutputValue>? OutputValues { get; set; }
}

public class Parameter
{
    public JObject? Default { get; set; }
    public List<JObject>? Range { get; set; }
    public int? MinRange { get; set; }
    public int? MaxRange { get; set; }
    public string? Type { get; set; }
}

public class OutputValue
{
    public string? DefaultColor { get; set; }
    public string? Display { get; set; }
    public int? MinRange { get; set; }
    public int? MaxRange { get; set; }
    public Tinting? Tinting { get; set; }
}

public class Tinting
{
    public string? Display { get; set; }
    public string? Color { get; set; }
    public float Transparency { get; set; }
    public JObject? LowerBound { get; set; }
    public JObject? UpperBound { get; set; }
}