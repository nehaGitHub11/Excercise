using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NehaExercise
{
    /// <summary>
    /// helper class
    /// </summary>
    public class Helper
    {
        //contains sorting options
        public enum SortingOptions
        {
            [Description("Low to High Price")]
            Low,
            [Description("High to Low Price")]
            High,
            [Description("A - Z sort on the Name")]
            Ascending,
            [Description("A - Z sort on the Name")]
            Descending,
            [Description("Popular")]
            Recommended

        }

        /// <summary>
        /// get token either from query string or request header
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetToken(HttpRequest request)
        {
            string retrieveToken = "";
            if (request.Query.ContainsKey("token"))
            {
                retrieveToken = request.Query["token"].ToString();
            }            

            if (string.IsNullOrWhiteSpace(retrieveToken))
            {
                retrieveToken = request.Headers.ContainsKey("token") ? request.Headers["token"].ToString() : "";
            }
           
            return retrieveToken;
        }

        /// <summary>
        /// This method will create GET api request with requested string url
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpClient"></param>
        /// <returns></returns>
        public static async Task<T> GetAPIRequestAsync<T>(HttpClient httpClient)
        {
            var response = await httpClient.GetAsync(string.Empty);
            if (response != null && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception("Cannot get list of products as the request is unauthorised");
            }
            var readContent = response != null ? await response.Content.ReadAsStringAsync() : "";
           
            return JsonConvert.DeserializeObject<T>(readContent);
        }

        /// <summary>
        /// post request and get the response with status,code and error
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="httpClient"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<(T1 content, bool isSuccess, HttpStatusCode code, string error)> 
                        PostAPIRequestAsync<T, T1>(HttpClient httpClient, T data)
        {
            httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(string.Empty, content);

            if (response == null)
            {
                return (default(T1), false, HttpStatusCode.InternalServerError, "Post response is null");
            }

            bool isSuccess = response.IsSuccessStatusCode;
            var readContent = response.StatusCode != HttpStatusCode.NoContent
                            || !isSuccess ? await response.Content.ReadAsStringAsync() : "";
            var error = readContent;

            var responseData = !isSuccess ? default : 
                        response.StatusCode == HttpStatusCode.NoContent 
                        ? JsonConvert.DeserializeObject<T1>(response.IsSuccessStatusCode.ToString().ToLower())
                        : JsonConvert.DeserializeObject<T1>(readContent);
            return (responseData, isSuccess, response.StatusCode, error);
        }

    }
}
