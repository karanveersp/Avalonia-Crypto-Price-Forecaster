using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RestSharp;

namespace Shared.Models;

public class GeckoClient : IGeckoClient
{
    private readonly string _baseUrl;

    public GeckoClient(HttpClient httpClient)
    {
        _baseUrl = "https://api.coingecko.com/api/v3";
    }

    public async Task<CoinsResponse[]> GetCoinsList()
    {
        using var client = new RestClient(_baseUrl);
        var req = new RestRequest("/coins/list");
        var res = await client.GetAsync<CoinsResponse[]>(req).ConfigureAwait(false);
        return res!;
    }

    public async Task<decimal> GetUsdPrice(string[] currencyIds, string[] vsCurrencies)
    {
        using var client = new RestClient(_baseUrl);
        var req = new RestRequest("/simple/price")
            .AddQueryParameter("ids", string.Join(',', currencyIds))
            .AddQueryParameter("vs_currencies", string.Join(',', vsCurrencies));
        var res = await client.GetAsync(req).ConfigureAwait(false);
        var strResp = res.Content;
        string pattern = @"(\d+\.?\d+)";

        Match match = Regex.Match(strResp!, pattern);

        return decimal.Parse(match.Groups[1].Value);
    }
}