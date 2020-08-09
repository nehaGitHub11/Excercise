using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace NehaExercise.Authentication
{
    public interface IAuthenticateRequest
    {
        bool IsSecure(HttpRequest request, IConfiguration config);
    }
}