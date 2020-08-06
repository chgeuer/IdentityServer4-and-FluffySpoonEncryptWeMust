namespace IdentityServer
{
    using System;
    using System.Collections.Generic;
    using Certes;
    using FluffySpoon.AspNet.EncryptWeMust;
    using FluffySpoon.AspNet.EncryptWeMust.Certes;
    using IdentityServer4.Models;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using Serilog.Events;
    using Serilog.Sinks.SystemConsole.Themes;
    using Addresses;

    public class STSProgram
    {
        public static int Main(string[] args)
        {
            Console.Title = "STS";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            try
            {
                Log.Information("Starting host...");
                CreateHostBuilder(args).Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHost CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel()
                        //.UseKestrel(kestrelServerOptions =>
                        //{
                        //    kestrelServerOptions.ListenAnyIP(
                        //        port: 80,
                        //        configure: lo => { lo.Protocols = HttpProtocols.Http1AndHttp2; }
                        //    );
                        //    kestrelServerOptions.ListenAnyIP(
                        //        port: 443,
                        //        configure: lo =>
                        //        {
                        //            lo.Protocols = HttpProtocols.Http1AndHttp2;
                        //            lo.UseHttps(configureOptions: o =>
                        //            {
                        //                //if (LetsEncryptRenewalService.Certificate is LetsEncryptX509Certificate x509Certificate)
                        //                //{
                        //                //    options.ConfigureHttpsDefaults(o =>
                        //                //    {
                        //                //        o.ServerCertificateSelector = (_a, _b) => x509Certificate.GetCertificate();
                        //                //    });
                        //                //}
                        //            });
                        //        }
                        //    );
                        //})
                        .UseUrls("http://*", "https://*")
                        .UseStartup<STSStartup>()
                        ;
;
                });

            var host = hostBuilder.Build();

            return host;
        }
    }
    

    public class STSStartup
    {
        public IWebHostEnvironment Environment { get; }

        public STSStartup(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // uncomment, if you want to add an MVC-based UI
            //services.AddControllersWithViews();

            services.AddFluffySpoonLetsEncrypt(options: new LetsEncryptOptions
            {
                Email = "foo@web.de",
                UseStaging = false,
                Domains = new[] { Address.EXTERNAL_DNS },
                TimeAfterIssueDateBeforeRenewal = TimeSpan.FromDays(7),
                TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30),
                RenewalFailMode = RenewalFailMode.LogAndRetry,
                CertificateSigningRequest = new CsrInfo
                {
                    Organization = "Christian Geuer-Pollmann",
                    OrganizationUnit = "Private",
                    State = "NRW",
                    CountryName = "Germany",
                    Locality = "DE",
                },
            });
            services.AddFluffySpoonLetsEncryptFileCertificatePersistence();
            services.AddFluffySpoonLetsEncryptFileChallengePersistence();

            var identityServerBuilder = services
                .AddIdentityServer(options =>
                {
                    options.EmitStaticAudienceClaim = true;
                })
                .AddInMemoryIdentityResources(Config.IdentityResources)
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryClients(Config.Clients)
                ;

            // not recommended for production - you need to store your key material somewhere secure
            identityServerBuilder.AddDeveloperSigningCredential();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // uncomment if you want to add MVC
            //app.UseStaticFiles();
            //app.UseRouting();

            app.UseFluffySpoonLetsEncrypt();

            //const string LetsEncryptChallengePath = "/.well-known/acme-challenge";
            //app.MapWhen(
            //    httpContext => !httpContext.Request.Path.StartsWithSegments(LetsEncryptChallengePath),
            //    appBuilder => { app.useH.UseHttpsRedirection(); }
            //);
            //app.MapWhen(
            //    httpContext => httpContext.Request.Path.StartsWithSegments(LetsEncryptChallengePath),
            //    appBuilder => { appBuilder.UseFluffySpoonLetsEncryptChallengeApprovalMiddleware(); }
            //);

            app.UseIdentityServer();

            // uncomment, if you want to add MVC
            //app.UseAuthorization();
            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapDefaultControllerRoute();
            //});
        }
    }

    public static class Config
    {
        internal static class Names
        {
            public static string ApiName => "api1";
        }

        public static IEnumerable<IdentityResource> IdentityResources => new IdentityResource[] {
            new IdentityResources.OpenId()
        };

        public static IEnumerable<ApiScope> ApiScopes => new ApiScope[] {
            new ApiScope(Names.ApiName, "My API")
        };

        public static IEnumerable<Client> Clients => new Client[]  {
            new Client
            {
                ClientId = "client",

                // no interactive user, use the clientid/secret for authentication
                AllowedGrantTypes = GrantTypes.ClientCredentials,

                // secret for authentication
                ClientSecrets = { new Secret("secret".Sha256()) },

                // scopes that client has access to
                AllowedScopes = { "openid", Names.ApiName }
            }
        };
    }
}