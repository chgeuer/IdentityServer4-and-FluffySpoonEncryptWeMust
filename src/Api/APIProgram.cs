namespace Api
{
    using System;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using Addresses;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.IdentityModel.Tokens;

    public class APIProgram
    {
        public static void Main(string[] args)
        {
            Console.Title = "API";

            // ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // IdentityModelEventSource.ShowPII = true;

            services
                .AddControllers();

            services
                .AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = Address.STS;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                    };
                    //options.TokenValidationParameters = new TokenValidationParameters
                    //{
                    //    // RequireSignedTokens = true,
                    //    // ValidateIssuerSigningKey = true,
                    //    // IssuerSigningKey = new ECDsaSecurityKey(ecdsa: Address.GetTokenSigningCertificate().GetECDsaPublicKey()),
                    //    ValidateIssuer = true,
                    //    IssuerValidator = new IssuerValidator()
                    //    IssuerSigningKeys = new[]
                    //    { 
                    //        new ECDsaSecurityKey(ecdsa: Address.GetTokenSigningCertificate().GetECDsaPublicKey())
                    //    },
                    //    IssuerSigningKeyValidator = (sk, st, tvp) =>
                    //    {
                    //        return true;
                    //    },
                    //};
                });

            services
                .AddAuthorization(options =>
                {
                    options.AddPolicy(ApiScopePolicyName, policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireClaim("scope", "api1");
                    });
                });
        }

        private static string ApiScopePolicyName = "ApiScope";

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints
                    .MapControllers()
                    .RequireAuthorization(ApiScopePolicyName);
            });
        }
    }

    [Authorize]
    [Route("showmemyidentity")]
    public class IdentityController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return new JsonResult(this.User.Claims.Select(c => $"type={c.Type} value={c.Value}"));
        }
    }
}
