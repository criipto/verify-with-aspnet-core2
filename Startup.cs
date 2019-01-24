using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json.Linq;

namespace VerifyWithAspNetCore2
{
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Integrate with Criipto Verify, part 1: 
            //  - configure core authentication
            //  - cookie schemes
            //  - OpenID Connect
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
            })
            .AddOpenIdConnect("CriiptoVerify", options =>
            {
                var authority = $"https://{Configuration["CriiptoVerify:DnsName"]}";
                options.Authority = authority;
                options.MetadataAddress = $"{authority}/.well-known/openid-configuration";
                options.ClientId = Configuration["CriiptoVerify:ClientId"];
                options.ClientSecret = Configuration["CriiptoVerify:ClientSecret"];
                options.CallbackPath = "/signin-criiptoverify";
                options.SignedOutCallbackPath = "/signout-criiptoverify";
                options.SignedOutRedirectUri = "/";
                options.ResponseType = Configuration["CriiptoVerify:ResponseType"] ?? "code";
                if (options.ResponseType == "code id_token")
                {
                    // Make the OIDC middleware fetch claims from the userinfo endpoint 
                    options.GetClaimsFromUserInfoEndpoint = true;
                    // - and add them all to the generated ClaimsPrincipal
                    options.ClaimActions.Add(new MapAllClaimsAction());
                }
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.NonceCookie.SameSite = SameSiteMode.None;
                options.ResponseMode = OpenIdConnectResponseMode.FormPost;
                options.Events = new OpenIdConnectEvents
                {                    
                    OnRedirectToIdentityProvider = (context) =>
                    {
                        context.Options.CorrelationCookie.SameSite = SameSiteMode.None;
                        context.Options.NonceCookie.SameSite = SameSiteMode.None;
                        // Integrate with Criipto Verify, to support more than 1 way to authenticate:
                        //  - select the most sensible default for your scenario
                        var acrValue = "urn:grn:authn:se:bankid:another-device";
                        // Have a button and/or link on your site for each kind of authentication that you want to support in your application, 
                        // and configure them with so they set the acrValue in a query parameter.
                        // This bit will then roundtrip that value to Criipto Verify dynamically:
                        if (context.Request != null 
                            && context.Request.Query != null 
                            && !String.IsNullOrWhiteSpace(context.Request.Query["acrValue"]))
                        {
                            acrValue = context.Request.Query["acrValue"];
                        }
                        context.ProtocolMessage.AcrValues = acrValue;
                        return Task.CompletedTask;
                    },
                    // Useful troubleshooting hooks follow (not all may be in use, it depends on the value configured for options.ResponseType above)
                    OnAuthorizationCodeReceived = context =>
                    {
                        var code = context.ProtocolMessage.Code;
                        return Task.CompletedTask;
                    },
                    OnTokenResponseReceived = context =>
                    {
                        var accessToken = context.ProtocolMessage.AccessToken;
                        var idToken = context.ProtocolMessage.IdToken;
                        return Task.CompletedTask;
                    },
                    OnUserInformationReceived = context =>
                    {
                        var userInfoClaims = 
                            context.User
                                .Children()
                                .OfType<JProperty>()
                                .Select(prop =>
                                    new { claimType = prop.Name, claimValues = prop.Values<string>().ToList() })
                                .ToList();
                        return Task.CompletedTask;
                    }
                };
            });
            // Integrate with Criipto Verify, part 2: 
            //  - configure a named authorization policy for use in [Authorize] attributes on controllers and/or action methods
            services.AddAuthorization(options =>
            {
                options.AddPolicy("CriiptoVerifyAuthenticatedUser", builder =>
                {
                    builder.AddAuthenticationSchemes("CriiptoVerify");
                    builder.RequireAuthenticatedUser();
                });
            });
            
            services.AddMvc();
            services.AddOptions();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            // Integrate with Criipto Verify, part 3: 
            // - activate the authentication subsystem as set up in the ConfigureServices stage. 
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
