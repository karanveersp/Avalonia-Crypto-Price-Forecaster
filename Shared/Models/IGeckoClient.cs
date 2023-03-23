using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shared.Models;

public interface IGeckoClient
{
    Task<CoinsResponse[]> GetCoinsList();
    Task<decimal> GetUsdPrice(string[] currencyIds, string[] vsCurrencies);
}