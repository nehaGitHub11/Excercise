using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NehaExercise.Filters;
using NehaExerciseModel;
using static NehaExercise.Helper;

namespace NehaExercise.Controllers
{
    /// <summary>
    /// This will provide you access to the answers for exercise
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ExerciseAuthentication]
    public class AnswersController : ControllerBase
    {
        private readonly ILogger<AnswersController> _logger;
        private readonly IConfiguration _config;
        private const string defaultUserName = "Neha";
        private readonly IHttpClientFactory _clientFactory;

        /// <summary>
        /// set up dependencies
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        public AnswersController(ILogger<AnswersController> logger,
            IConfiguration config, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _config = config;
            _clientFactory = clientFactory;
        }

        /// <summary>
        /// This GET method will return user and 
        /// token if token is passed in Request header or in query string
        /// </summary>
        /// <returns></returns>
        [HttpGet("user")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Get(string token)
        {
            _logger.LogInformation("Get User Entered");

            string getToken = RetrieveTokenFromDiffWays(token);

            string userName = "";
            if (_config.GetSection("ApiSettings:UserName") != null)
            {
                userName = _config.GetValue<string>("ApiSettings:UserName");
                _logger.LogInformation("App settings found for userName");
            }
            if (string.IsNullOrEmpty(userName)) userName = defaultUserName;
            var user = new UserResponse() { Name = userName, Token = getToken };
            return Ok(user);
        }

        private string RetrieveTokenFromDiffWays(string token)
        {
            string getToken = token;
            if (string.IsNullOrWhiteSpace(getToken)) // token is not passed in query string
            {
                getToken = Helper.GetToken(Request);
            }

            if (string.IsNullOrWhiteSpace(getToken))
            {
                if (_config.GetSection("ApiSettings:defaultToken") != null)
                {
                    getToken = _config.GetValue<string>("ApiSettings:defaultToken");
                    _logger.LogInformation("Retrieved token from app settings");
                }
            }

            return getToken;
        }

        /// <summary>
        /// This method will provide sorted list of products based on sortOption provided
        /// </summary>
        /// <param name="token"></param>
        /// <param name="sortOption"></param>
        /// <returns></returns>
        [HttpGet("sort")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Product>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> GetSortedProducts(string token, string sortOption)
        {
            string getToken = RetrieveTokenFromDiffWays(token);
            List<Product> allProducts = null;
            List<Product> sortedProducts = null;
            if (string.IsNullOrEmpty(sortOption))
            {
                _logger.LogInformation("No sorting option provided");
                return StatusCode(422, "No sorting mechanism provided");
            }

            allProducts = await GetAllProductsInfo(getToken);

            if (allProducts == null || allProducts.Count == 0)
            {
                _logger.LogInformation("No product found to sort");
                return StatusCode(204, "No product found to sort");
            }

            var enumSort = (SortingOptions)System.Enum.Parse(typeof(SortingOptions), sortOption, true);

            switch (enumSort)
            {
                case SortingOptions.Low:
                    {
                        sortedProducts = allProducts.OrderBy(p => p.Price).ToList();
                        break;
                    }
                case SortingOptions.High:
                    {
                        sortedProducts = allProducts.OrderByDescending(p => p.Price).ToList();
                        break;
                    }
                case SortingOptions.Ascending:
                    {
                        sortedProducts = allProducts.OrderBy(p => p.Name).ToList();
                        break;
                    }
                case SortingOptions.Descending:
                    {
                        sortedProducts = allProducts.OrderByDescending(p => p.Name).ToList();
                        break;
                    }
                case SortingOptions.Recommended:
                    {
                        //get shopperHistory
                        List<ShopperHistory> getshopperHistory = null;
                        using (var client = _clientFactory.CreateClient())
                        {
                            client.BaseAddress = new Uri($"http://dev-wooliesx-recruitment.azurewebsites.net/api/resource/shopperHistory?token= {getToken}");
                            getshopperHistory = await Helper.GetAPIRequestAsync<List<ShopperHistory>>(client);
                        }

                        if (getshopperHistory == null || getshopperHistory.Count() == 0)
                        {
                            return StatusCode(204, "No shopper history found to sort");
                        }

                        var popularProducts = getshopperHistory
                                        .SelectMany(h => h.Products, (c, p) => new { c.CustomerId, productName = p.Name })
                                          .GroupBy(g => g.productName)
                                          .Select(i => new { ProductName = i.Key, Popularity = i.Count() });

                        sortedProducts = popularProducts
                            .Join(
                                allProducts,
                                p => p.ProductName,
                                a => a.Name,
                                (p, a) => new
                                {
                                    p.Popularity,
                                    a
                                })
                            .OrderByDescending(a => a.Popularity)
                            //.ThenBy(p => p.a.Name)     // not needed still adding for sake of completeness.
                            .Select(p => p.a)
                            .ToList();
                        break;
                    }
                default:
                    return StatusCode(422, "No sorting mechanism provided");
            }

            return Ok(sortedProducts);
        }

        private async Task<List<Product>> GetAllProductsInfo(string getToken)
        {
            List<Product> getAllProducts = null;

            //get products first
            using (var client = _clientFactory.CreateClient())
            {
                client.BaseAddress = new Uri($"http://dev-wooliesx-recruitment.azurewebsites.net/api/resource/products?token= {getToken}");
                getAllProducts = await Helper.GetAPIRequestAsync<List<Product>>(client);
            }

            return getAllProducts;
        }

        [HttpPost("trolleyTotal")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Trolley))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTrolleyTotal([FromBody] Trolley trolleyStuff)
        {
            var getToken = "41bee812-7a3b-46c6-a306-5bd481d957a2";
            bool useCustomCalculator = false;
            if (_config.GetSection("ApiSettings:UseCustomCalculator") != null)
            {
                useCustomCalculator = _config.GetValue<bool>("ApiSettings:UseCustomCalculator");
            }

            decimal TrolleyTotal;
            if (useCustomCalculator)
            {
                TrolleyTotal = TrolleyCalculator.CalculateTrolleyTotal(trolleyStuff);
                return Ok(TrolleyTotal);
            }
            else
            {
                decimal content; bool isSuccess; HttpStatusCode code; string error= "";
                using (var client = _clientFactory.CreateClient())
                {
                    client.BaseAddress = new Uri($"http://dev-wooliesx-recruitment.azurewebsites.net/api/resource/trolleyCalculator?token= {getToken}");
                    (content, isSuccess, code, error) = await Helper.PostAPIRequestAsync<Trolley, decimal>(client, trolleyStuff);
                }

                _logger.LogInformation($"GetTrolleyCalcResponse : isSuccess {isSuccess} ErrorWhilePostingDataToWooliesXCalc : {error}");
                if (!isSuccess) return StatusCode((int)code,$"ErrorWhilePostingDataToWooliesXCalc {error}");
                TrolleyTotal = content;
                return Ok(TrolleyTotal);
            }

        }
    }
}
