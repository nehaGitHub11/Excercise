using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NehaExercise.Authentication
{
    public class NoAuthenticationRequest : IAuthenticateRequest
    {
        public bool IsSecure(HttpRequest request, IConfiguration config)
        {
            return true;
        }
    }
}
