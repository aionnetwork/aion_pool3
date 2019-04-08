using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Miningcore.Api.Extensions;
using Miningcore.Api.Responses;
using Miningcore.Configuration;
using Miningcore.Extensions;
using Miningcore.Persistence;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Repositories;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Autofac;

namespace Miningcore.Api.Controllers
{
    [Route("api/coin/")]
    [ApiController]
    public class CoinInfoController : ControllerBase
    {
        public CoinInfoController(IComponentContext ctx)
        {
            this.coinInfoRepo = ctx.Resolve<ICoinInfoRepository>();
            this.cf = ctx.Resolve<IConnectionFactory>();
        }

        private ICoinInfoRepository coinInfoRepo;
        private IConnectionFactory cf;
        private static readonly JsonSerializer serializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        [HttpGet("{coin}")]
        public async Task<CoinInfoResponse> GetCoinInfo(string coin)
        {
            var coinType = (CoinFamily)CoinFamily.Parse(typeof(CoinFamily), coin, true);
            var coinInfo = await cf.Run(con => coinInfoRepo.GetCoinInfo(con, coinType.ToString()));
            if (coinInfo == null || DateTime.Now.Subtract(coinInfo.Updated).TotalMinutes > 5)
            {
                coinInfo = GetCoinInfoFromCryptoCompare(coinType);
                cf.RunTx((con, tx) => coinInfoRepo.AddCoinInfo(con, tx, coinInfo));
            }

            var response = new CoinInfoResponse
            {
                CoinType = coinInfo.CoinType.ToString(),
                Name = coinInfo.Name,
                PriceBTC = coinInfo.PriceBTC,
                PriceUSD = coinInfo.PriceUSD
            };

            return response;
        }

        private CoinInfo GetCoinInfoFromCryptoCompare(CoinFamily coinType)
        {
            var url = "https://min-api.cryptocompare.com/data/price?fsym=" + coinType.ToString().ToUpper() + "&tsyms=BTC,USD";
            var json = new WebClient().DownloadString(url);
            var response = JsonConvert.DeserializeObject<CryoptoCompareResponse>(json);
            return new CoinInfo
            {
                CoinType = coinType.ToString(),
                Name = coinType.ToString(),
                CoinMarketCapId = 0,
                PriceUSD = response.USD,
                PriceBTC = response.BTC,
                Updated = DateTime.Now
            };
        }

        private async Task SendJsonAsync(HttpContext context, object response)
        {
            context.Response.ContentType = "application/json";

            // add CORS headers
            context.Response.Headers.Add("Access-Control-Allow-Origin", new StringValues("*"));
            context.Response.Headers.Add("Access-Control-Allow-Methods", new StringValues("GET, POST, DELETE, PUT, OPTIONS, HEAD"));

            using (var stream = context.Response.Body)
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    serializer.Serialize(writer, response);

                    // append newline
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                }
            }
        }
    }
}