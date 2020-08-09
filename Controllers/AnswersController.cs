using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// set up dependencies
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        public AnswersController(ILogger<AnswersController> logger,
            IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// This GET method will return user and token if token is passed in Request header
        /// For ex- token : "454444454-4545454"
        /// </summary>
        /// <returns></returns>
        [HttpGet("user")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Get(string token)
        {
            _logger.LogInformation("Get User Entered");

            string getToken = token;
            if (string.IsNullOrWhiteSpace(token)) // token is not passed in query string
            {
                getToken = Helper.GetToken(Request);
            }

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

        /// <summary>
        /// This method will provide sorted list of products based on sortOption provided
        /// </summary>
        /// <param name="token"></param>
        /// <param name="sortOption"></param>
        /// <returns></returns>
        [HttpGet("sort")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Product>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSortedProducts(string token, string sortOption)
        {
            string getToken = token;
            List<Product> allProducts = null;
            List<Product> sortedProducts = null;
            if (string.IsNullOrWhiteSpace(getToken)) // token is not passed in query string
            {
                getToken = Helper.GetToken(Request);
            }
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

            var enumSort = (SortingOptions)System.Enum.Parse(typeof(SortingOptions), sortOption,true);

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
                        //get products first
                        using var client = new HttpClient
                        {
                            BaseAddress = new Uri($"http://dev-wooliesx-recruitment.azurewebsites.net/api/resource/shopperHistory?token= {getToken}")
                        };
                        var getshopperHistory = await Helper.GetAPIRequestAsync<List<ShopperHistory>>(client);

                        if (getshopperHistory == null || getshopperHistory.Count == 0)
                        {
                            return StatusCode(204, "No shopper history found to sort");
                        }

                        var popularProducts = getshopperHistory.SelectMany(h=>h.Products,(c,p)=> new { c.CustomerId,productName = p.Name})
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
                            .ThenBy(p => p.a.Name)     // not needed still adding for sake of completeness.
                            .Select(p => p.a)
                            .ToList();
                        break;
                    }
                default:
                    return StatusCode(422, "No sorting mechanism provided");
            }

            return Ok(sortedProducts);
        }

        private static async Task<List<Product>> GetAllProductsInfo(string getToken)
        {
            List<Product> getAllProducts = null;
            //get products first
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://dev-wooliesx-recruitment.azurewebsites.net/api/resource/products?token= {getToken}");
                getAllProducts = await Helper.GetAPIRequestAsync<List<Product>>(client);
            }

            return getAllProducts;
        }

        //[HttpGet("trolleyTotal")]
        //[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //public async Task<IActionResult> GetTrolleyTotal(string token)
        //{
        //    string getToken = "";
          
        //    if (string.IsNullOrWhiteSpace(token)) // token is not passed in query string
        //    {
        //        getToken = Helper.GetToken(Request);
        //    }
        //}
        }
}
