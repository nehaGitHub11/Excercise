using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NehaExercise.Authentication;
using NehaExercise.Filters;

namespace NehaExercise
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;
        private static string _contentRootPath = string.Empty;
        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger;
            _contentRootPath = env.ContentRootPath;

            var builder = new ConfigurationBuilder()
            .SetBasePath(_contentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogInformation("StartUp : Entered ConfigureServices");

            IMvcCoreBuilder mvcCoreBuilder;

            //mvcCoreBuilder = services.AddMvcCore(options => {
            //    options.Filters.Add(new ExerciseAuthenticationFilter());
            //});

            mvcCoreBuilder = services.AddMvcCore();

            mvcCoreBuilder
               .AddFormatterMappings()
               .AddApiExplorer()
               .AddCors()
               .AddDataAnnotations()
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddControllers();

            services.AddSingleton<IAuthenticateRequest, TokenAuthenticationRequest>();
            services.AddSwaggerGen(action =>
            {
                action.SwaggerDoc("NehaExerciseOpenApiSpec", new OpenApiInfo()
                {
                    Title = "Neha Exercise API",
                    Version = "v1"
                });
                string xmlpath = Path.Combine(AppContext.BaseDirectory, "NehaExerciseAPI.xml");
                action.IncludeXmlComments(xmlpath);
            });

            services.AddMvc()
              .AddNewtonsoftJson();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var result = new BadRequestObjectResult(context.ModelState);

                    result.ContentTypes.Add(MediaTypeNames.Application.Json);

                    return result;
                };
            });
            _logger.LogInformation("StartUp : Exit ConfigureServices");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            _logger.LogInformation("StartUp : Entered Configure");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
               // app.UseExceptionHandler("/error-local-development");
            }
            else
            {
                app.UseHsts();
            }
            app.UseExceptionHandler("/error");

            app.UseHttpsRedirection();

            app.UseSwagger();
            app.UseSwaggerUI(setupAction =>
            {
                setupAction.SwaggerEndpoint("/swagger/NehaExerciseOpenApiSpec/swagger.json", "NehaExerciseAPI");
                setupAction.RoutePrefix = string.Empty;
            });          

            app.UseRouting();
            app.UseCors(x => x
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

            //app.UseAuthentication();
            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            _logger.LogInformation("StartUp : Exit Configure");
        }
    }
}
