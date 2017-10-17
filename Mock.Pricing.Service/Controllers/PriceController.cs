using System.Collections.Generic;
using System.Web.Http;
using Mock.Pricing.Service.Base.Model;
using NLog;

namespace Mock.Pricing.Service.Controllers
{
    public class PriceController : ApiController
    {
        /// <summary>
        /// Sample interface for getting all prices
        /// </summary>
        [HttpGet]
        [Route("api/prices")]
        public IHttpActionResult GetPrices()
        {
            return Ok(prices);
        }


        /// <summary>
        ///     Used to log messages.
        /// </summary>
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly List<Price> prices = new List<Price>
        {
            new Price("WD", 57.274),
            new Price("DA", 57.174),
            new Price("BOM", 57.076),
            new Price("Jan-16", 56.979),
            new Price("Feb-16", 56.884),
            new Price("Mar-16", 56.790),
            new Price("Apr-16", 56.698),
            new Price("May-16", 56.608),
            new Price("Jun-16", 55.520),
            new Price("Jul-16", 56.420),
            new Price("Aug-16", 54.912),
            new Price("Sep-16", 55.815),
            new Price("Oct-16", 55.714),
            new Price("Nov-16", 55.610),
            new Price("Dec-16", 55.502),
            new Price("Jan-17", 55.979),
            new Price("Feb-17", 55.884),
            new Price("Mar-17", 55.790),
            new Price("Apr-17", 55.698),
            new Price("May-17", 54.608),
            new Price("Jun-17", 54.608),
            new Price("Jul-17", 54.520),
            new Price("Aug-17", 54.912),
            new Price("Sep-17", 54.815),
            new Price("Oct-17", 55.714),
            new Price("Nov-17", 55.610),
            new Price("Dec-17", 55.502),
            new Price("2016 Q1", 55.391),
            new Price("2016 Q2", 55.276),
            new Price("2016 Q3", 55.157),
            new Price("2016 Q4", 55.034),
            new Price("Sum16", 54.910),
            new Price("Win16", 54.798),
            new Price("Cal16", 54.700),
            new Price("Cal17", 54.700),
            new Price("Cal18", 54.700)
        };
    }
}