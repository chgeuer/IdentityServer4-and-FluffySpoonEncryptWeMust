﻿namespace IdentityServer
{
    using System;
    using System.Collections.Generic;
    using IdentityServer4.Models;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using Serilog.Events;
    using Serilog.Sinks.SystemConsole.Themes;

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
                        .PreferHostingUrls(false)
                        .UseUrls("http://*", "https://*")
                        .UseStartup<STSStartup>()
                        .UseKestrel();
                }
            );

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
            

            var identityServerBuilder = services
                .AddIdentityServer(options =>
                {
                    options.EmitStaticAudienceClaim = true;
                })
                .AddInMemoryIdentityResources(Config.IdentityResources)
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryClients(Config.Clients)
                // .AddSigningCredential(new RsaSecurityKey(RSA.Create()), signingAlgorithm: IdentityServerConstants.RsaSigningAlgorithm.RS256)
                .AddDeveloperSigningCredential() // not recommended for production - you need to store your key material somewhere secure
                ;
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