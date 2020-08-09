using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NehaExercise.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NehaExercise.Filters
{
    public class ExerciseAuthenticationAttribute : TypeFilterAttribute
    {
        public ExerciseAuthenticationAttribute() : base(typeof(ExerciseAuthenticationFilter))
        {
        }
    }

    public class ExerciseAuthenticationFilter : IAuthorizationFilter
    {
        private readonly IConfiguration config;
        private readonly IAuthenticateRequest auth;
        private readonly ILogger<ExerciseAuthenticationFilter> logger;

        public ExerciseAuthenticationFilter(IConfiguration config, 
                                            IAuthenticateRequest auth,
                                            ILogger<ExerciseAuthenticationFilter> logger)
        {
            this.config = config;
            this.auth = auth;
            this.logger = logger;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (auth.IsSecure(context.HttpContext.Request, config) == false)
            {
                logger.LogError($"Unauthorised Access.");
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
