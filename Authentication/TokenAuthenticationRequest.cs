using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NehaExercise.Authentication
{
    public class TokenAuthenticationRequest : IAuthenticateRequest
    {
        private const string defaultToken = "41bee812-7a3b-46c6-a306-5bd481d957a2";

        /// <summary>
        /// check token
        /// </summary>
        /// <param name="request"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool IsSecure(HttpRequest request, IConfiguration config)
        {
            string retrievedToken = Helper.GetToken(request);

            if (string.IsNullOrWhiteSpace(retrievedToken)) return false;

#region ValidateToken
            string validToken = "";
            if (config.GetSection("ApiTokens:token") != null)
            {
                validToken = config.GetValue<string>("ApiTokens:token");
            }

            if (string.IsNullOrEmpty(validToken)) validToken = defaultToken;

            if (string.Compare(retrievedToken,validToken,false) == 0) return true;

#endregion

            return false;
        }
    }
}
