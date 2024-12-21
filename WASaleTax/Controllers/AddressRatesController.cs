using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WASalesTax.Models;
using WASalesTax.Parsing;

namespace WASalesTax.Controllers
{
    [ApiController]
    public class AddressRatesController : ControllerBase
    {
        private readonly WashingtonStateContext _context;

        public AddressRatesController(WashingtonStateContext context)
        {
            _context = context;
            // Disable to improve read-only performance and reduce memory consumption.
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        /// <summary>
        /// Find a sales tax rate using the same API provided by the State of Washington's Department of Revenue.
        /// </summary>
        /// <param name="output">The format of the response, either "xml" or "text".</param>
        /// <param name="addr">The street address of the customer/point of sale. (ex. "6500 Linderson way") Please do not include unit, office, or apt numbers. Just the simple physical address.</param>
        /// <param name="city">The city of that the customer/point of resides in. (ex. "Olympia")</param>
        /// <param name="zip">The 5 digit Zip Code. (ex. "98501") Plus4 Zip Codes are optional. (ex. "98501-6561" or "985016561" )</param>
        /// <returns> An XML or string formatted tax rate object. Where the "rate" field is the total sale tax to collect.</returns>
        /// <response code="200">Returns a tax rate.</response>
        /// <response code="400">Returns a string discribing an invalid request.</response>
        [HttpGet("AddressRates.aspx")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ContentResult> LegacyLookupAsync(string output, string addr, string city, string zip)
        {
            // Using a contentresult here because we want to return Xml without using the built in XML serializer so that we have complete control over the format and content of the response.
            var response = new ContentResult();

            bool useXml = false;
            if (output is "xml")
            {
                useXml = true;
            }

            if (useXml)
            {
                response.ContentType = "text/xml; charset=utf-8";
                response.Content += "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n";
            }
            else
            {
                response.ContentType = "text/html; charset=utf-8";
            }

            zip = zip.Trim();

            // Fail fast on invalid zip codes.
            if (!string.IsNullOrWhiteSpace(zip) && (zip.Length == 5 || zip.Length == 9 || zip.Length == 10))
            {
                ShortZip matchingZip;

                // Find a representitive zip code entry as a starting place.
                switch (zip.Length)
                {
                    case 5:
                        matchingZip = await _context.ZipCodes.Where(x => x.Zip == zip).FirstOrDefaultAsync();
                        break;
                    case 9:
                        matchingZip = await _context.ZipCodes.Where(x => x.Zip == zip.Substring(0, 5)).FirstOrDefaultAsync();
                        break;
                    case 10:
                        zip = zip.Replace("-", string.Empty);
                        matchingZip = await _context.ZipCodes.Where(x => x.Zip == zip.Substring(0, 5)).FirstOrDefaultAsync();
                        break;
                    default:
                        matchingZip = null;
                        break;
                }

                // If no zip code if found return an invalid response.
                if (matchingZip is null)
                {
                    if (useXml)
                    {
                        response.Content += "<response loccode=\"\" localrate=\"\" rate=\"\" code=\"4\" debughint=\"Invalid ZIP\"><addressline/><rate/></response>";
                    }
                    else
                    {
                        response.Content += "LocationCode=-1 Rate=-1 ResultCode=4 debughint=Invalid ZIP";
                    }
                    response.StatusCode = 400;
                }
                else if (string.IsNullOrWhiteSpace(addr) && zip.Length == 5)
                {
                    // 5 digit ZIP only, no address provided.
                    var rate = await _context.TaxRates.Where(x => x.LocationCode == matchingZip.LocationCode).FirstOrDefaultAsync();

                    if (useXml)
                    {
                        response.Content += $"<response loccode=\"{rate.LocationCode}\" localrate=\"{rate.Local:.000}\" rate=\"{rate.Rate:.000}\" code=\"5\" xmlns=\"\"><addressline code=\"{rate.LocationCode}\" state=\"WA\" zip=\"{matchingZip.Zip}\" period=\"{Period.CurrentPeriod().PeriodLit}\" rta=\"\" ptba=\"\" cez=\"\" />{rate.ToXML()}</response>";
                    }
                    else
                    {
                        response.Content += $"LocationCode={rate.LocationCode} Rate={rate.Rate:.000} ResultCode=3";
                    }
                    response.StatusCode = 200;
                    return response;
                }
                else
                {
                    List<AddressRange> relatedAddressRanges;

                    if (zip.Length == 9)
                    {
                        var plus4 = zip[5..];
                        relatedAddressRanges = await _context.AddressRanges.Where(x => x.ZipCode == matchingZip.Zip && x.ZipCodePlus4 == plus4).ToListAsync();

                        // Skip address parsing if there's only one matching address range for the 9 digit ZIP.
                        if (relatedAddressRanges.Count == 1)
                        {
                            var match = relatedAddressRanges.FirstOrDefault();

                            var rate = await _context.TaxRates.Where(x => x.LocationCode == match.LocationCode).FirstOrDefaultAsync();

                            if (useXml)
                            {
                                response.Content += $"<response loccode=\"{rate.LocationCode}\" localrate=\"{rate.Local:.000}\" rate=\"{rate.Rate:.000}\" code=\"1\" xmlns=\"\">{match.ToXML()}{rate.ToXML()}</response>";
                            }
                            else
                            {
                                response.Content += $"LocationCode={rate.LocationCode} Rate={rate.Rate:.000} ResultCode=3";
                            }
                            response.StatusCode = 200;
                            return response;
                        }
                    }
                    else
                    {
                        relatedAddressRanges = await _context.AddressRanges.Where(x => x.ZipCode == matchingZip.Zip).ToListAsync();
                    }

                    // Fail fast if no address ranges for this zip can be found.
                    if (relatedAddressRanges is null || relatedAddressRanges.Count == 0)
                    {
                        if (useXml)
                        {
                            response.Content += "<response loccode=\"\" localrate=\"\" rate=\"\" code=\"4\" debughint=\"Invalid ZIP\"><addressline/><rate/></response>";
                        }
                        else
                        {
                            response.Content += "LocationCode=-1 Rate=-1 ResultCode=4 debughint=Invalid ZIP";
                        }
                        response.StatusCode = 400;
                    }
                    else
                    {
                        // Parse the street address and find a similar address range.
                        var parsedStreetAddress = new AddressLineTokenizer(addr);

                        if (!string.IsNullOrWhiteSpace(parsedStreetAddress.Street.Lexum))
                        {
                            AddressRange match = null;
                            double score = -3;
                            double mscore;

                            // Score the potential matches and select the highest rated.
                            foreach (var canidate in relatedAddressRanges)
                            {
                                if ((mscore = parsedStreetAddress.Match(canidate)) > score)
                                {
                                    match = canidate;
                                    score = mscore;
                                }
                            }

                            // If the score is to low or no match is found fail out.
                            if (null == match || score < -0.1)
                            {
                                if (useXml)
                                {
                                    response.Content += "<response loccode=\"\" localrate=\"\" rate=\"\" code=\"3\" debughint=\"Address not found\"><addressline/><rate/></response>";
                                }
                                else
                                {
                                    response.Content += "LocationCode=-1 Rate=-1 ResultCode=3 debughint=Invalid ZIP";
                                }
                                response.StatusCode = 404;
                            }
                            else
                            {
                                // Return the tax rate for the matching address range.
                                var rate = await _context.TaxRates.Where(x => x.LocationCode == match.LocationCode).FirstOrDefaultAsync();

                                if (useXml)
                                {
                                    response.Content += $"<response loccode=\"{rate.LocationCode}\" localrate=\"{rate.Local:.000}\" rate=\"{rate.Rate:.000}\" code=\"3\" xmlns=\"\">{match.ToXML()}{rate.ToXML()}</response>";
                                }
                                else
                                {
                                    response.Content += $"LocationCode={rate.LocationCode} Rate={rate.Rate:.000} ResultCode=3";
                                }
                                response.StatusCode = 200;
                            }
                        }
                    }
                }
            }
            else
            {
                if (useXml)
                {
                    response.Content += "<response loccode=\"\" localrate=\"\" rate=\"\" code=\"4\" debughint=\"Invalid ZIP\"><addressline/><rate/></response>";
                }
                else
                {
                    response.Content += "LocationCode=-1 Rate=-1 ResultCode=4 debughint=Invalid ZIP";
                }
                response.StatusCode = 400;
            }

            return response;
        }

        /// <summary>
        /// Find a sale tax rate in Washington State.
        /// </summary>
        /// <param name="addr">The street address of the customer/point of sale. (ex. "6500 Linderson way') Please do not include complex information like unit, office, or apt numbers.</param>
        /// <param name="zip">The zip code of the customer/point of sale. (ex. "98501") Zip Plus4 Code are also accepted (ex. "98501-6561" or "985016561" ) for better accuracy.</param>
        /// <returns> A JSON formatted tax rate object. Where the "rate" field is the total sale tax to collect.</returns>
        /// <response code="200">Returns a tax rate.</response>
        /// <response code="400">Returns a string discribing an invalid request.</response>
        [Produces("application/json")]
        [HttpGet("AddressRates")]
        [ProducesResponseType(typeof(TaxRate), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ModernizedLookupAsync(string addr, string zip)
        {
            if (!string.IsNullOrWhiteSpace(zip) && (zip.Length == 5 || zip.Length == 9 || zip.Length == 10))
            {
                ShortZip matchingZip;

                switch (zip.Length)
                {
                    case 5:
                        matchingZip = await _context.ZipCodes.Where(x => x.Zip == zip).FirstOrDefaultAsync();
                        break;
                    case 9:
                        matchingZip = await _context.ZipCodes.Where(x => x.Zip == zip.Substring(0, 5)).FirstOrDefaultAsync();
                        break;
                    case 10:
                        zip = zip.Replace("-", string.Empty);
                        matchingZip = await _context.ZipCodes.Where(x => x.Zip == zip.Substring(0, 5)).FirstOrDefaultAsync();
                        break;
                    default:
                        matchingZip = null;
                        break;
                }

                if (matchingZip is null)
                {
                    return BadRequest(new ProblemDetails { Status = 400, Detail = "Invalid ZIP Code. Please verify that the Zip code you submitted is real and formatted correctly.", Title = "Invalid ZIP Code" });
                }
                else if (string.IsNullOrWhiteSpace(addr) && zip.Length == 5)
                {
                    // 5 digit ZIP only, no address provided.
                    var rate = await _context.TaxRates.Where(x => x.LocationCode == matchingZip.LocationCode).FirstOrDefaultAsync();

                    return Ok(rate);
                }
                else
                {
                    List<AddressRange> relatedAddressRanges;

                    if (zip.Length == 9)
                    {
                        var plus4 = zip[5..];
                        relatedAddressRanges = await _context.AddressRanges.Where(x => x.ZipCode == matchingZip.Zip && x.ZipCodePlus4 == plus4).ToListAsync();

                        // Skip address parsing if there's only one matching address range for the 9 digit ZIP.
                        if (relatedAddressRanges.Count == 1)
                        {
                            var match = relatedAddressRanges.FirstOrDefault();

                            var rate = await _context.TaxRates.Where(x => x.LocationCode == match.LocationCode).FirstOrDefaultAsync();

                            return Ok(rate);
                        }
                    }
                    else
                    {
                        relatedAddressRanges = await _context.AddressRanges.Where(x => x.ZipCode == matchingZip.Zip).ToListAsync();
                    }

                    if (relatedAddressRanges is null || relatedAddressRanges.Count == 0)
                    {
                        return BadRequest(new ProblemDetails { Status = 400, Detail = "Invalid ZIP Code. Please verify that the Zip code you submitted is real and formatted correctly.", Title = "Invalid ZIP Code" });
                    }
                    else
                    {
                        var parsedStreetAddress = new AddressLineTokenizer(addr);

                        if (!string.IsNullOrWhiteSpace(parsedStreetAddress.Street.Lexum))
                        {
                            AddressRange match = null;
                            double score = -3;
                            double mscore;

                            foreach (var canidate in relatedAddressRanges)
                            {
                                if ((mscore = parsedStreetAddress.Match(canidate)) > score)
                                {
                                    match = canidate;
                                    score = mscore;
                                }
                            }
                            if (null == match || score < -0.1)
                            {
                                return BadRequest(new ProblemDetails { Status = 400, Detail = "No matching Addresses found", Title = "Address Not Found" });
                            }
                            else
                            {
                                var rate = await _context.TaxRates.Where(x => x.LocationCode == match.LocationCode).FirstOrDefaultAsync();

                                return Ok(rate);
                            }
                        }
                    }
                }

                return BadRequest(new ProblemDetails { Status = 400, Detail = "Invalid ZIP Code. Please verify that the Zip code you submitted is real and formatted correctly.", Title = "Invalid ZIP Code" });
            }
            else
            {
                return BadRequest(new ProblemDetails { Status = 400, Detail = "Invalid ZIP Code. Please verify that the Zip code you submitted is real and formatted correctly.", Title = "Invalid ZIP Code" });
            }
        }

        /// <summary>
        /// Find a sale tax rate in Washington State.
        /// </summary>
        /// <param name="houseNumber">The leading number in the street address (ex. 6500 in the address "6500 Linderson Way SW")</param>
        /// <param name="streetName"> The name of the street. (ex. "Linderson Way SW" in "6500 Linderson Way SW") Do not include the house number or the unit/suite/apt number.</param>
        /// <param name="shortZipCode"> The 5 digit Zip Code. (ex. "98501")</param>
        /// <param name="zipPlus4"> Plus4 Zip Codes are optional. (ex. "6561" from the complete zip code "98501-6561") </param>
        /// <response code="200">Returns a tax rate.</response>
        /// <response code="400">Returns a string discribing an invalid request.</response>
        [Produces("application/json")]
        [HttpGet("PreciseRate")]
        [ProducesResponseType(typeof(TaxRate), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PreciseLookupAsync(int houseNumber, string streetName, string shortZipCode, string zipPlus4)
        {
            if (string.IsNullOrWhiteSpace(shortZipCode))
            {
                return BadRequest("No zip code provided.");
            }

            if (!string.IsNullOrWhiteSpace(zipPlus4))
            {
                var result = await _context.AddressRanges.Where(x => x.ZipCode == shortZipCode && x.ZipCodePlus4 == zipPlus4).FirstOrDefaultAsync();

                var rate = await _context.TaxRates.Where(x => x.LocationCode == result.LocationCode).FirstOrDefaultAsync();

                return Ok(rate);
            }

            if (string.IsNullOrWhiteSpace(streetName))
            {
                return BadRequest("No street name provided.");
            }
            else
            {
                streetName = streetName.Trim().ToUpperInvariant();

                var canidates = await _context.AddressRanges.Where(x => (x.Street == streetName) && (x.ZipCode == shortZipCode)).ToListAsync();

                foreach (var canidate in canidates)
                {
                    if (canidate.AddressRangeUpperBound is null || canidate.AddressRangeLowerBound is null)
                    {
                        // Skip this canidate.
                        continue;
                    }

                    var high = canidate.AddressRangeUpperBound ?? 0;
                    var low = canidate.AddressRangeLowerBound ?? 0;

                    if (high >= houseNumber && low <= houseNumber)
                    {
                        var rate = await _context.TaxRates.Where(x => x.LocationCode == canidate.LocationCode).FirstOrDefaultAsync();

                        return Ok(rate);
                    }
                }

                return BadRequest("Could not locate this street name and house number.");
            }
        }
    }
}
