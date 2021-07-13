using IdentityModel;
using ImageGallery.Client.HttpHandlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace ImageGallery.Client
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews()
                 .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

            services.AddAuthorization(options => {
                options.AddPolicy("CanOrderFrame", policyBuilder => {
                    policyBuilder.RequireAuthenticatedUser();
                    policyBuilder.RequireClaim("country", "be");
                    policyBuilder.RequireClaim("subscriptionlevel", "PayingUser");
                });
            });

            services.AddHttpContextAccessor();
            services.AddTransient<BearerTokenHandler>();
            
            // create an HttpClient used for accessing the API
            services.AddHttpClient("APIClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:44311/");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            }).AddHttpMessageHandler<BearerTokenHandler>();

            // create an HttpClient used for accessing the API
            services.AddHttpClient("IDPClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:44336/");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            });

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,options=> 
                {
                    options.AccessDeniedPath = "/Authorization/AccessDenied";
                })
                .AddOpenIdConnect(options=>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = "https://localhost:44336/";
                    options.ClientId = "imagegalleryclient";
                    options.ResponseType = "code";
                     options.UsePkce = true;
                    //options.CallbackPath = new PathString("...")
                    //options.Scope.Add("openid");
                    //options.Scope.Add("profile");
                    options.Scope.Add("address");
                    options.Scope.Add("roles");
                    options.Scope.Add("imagegalleryapi");
                    options.Scope.Add("country");
                    options.Scope.Add("subscriptionlevel");
                    options.Scope.Add("offline_access");

                    // options.ClaimActions.Remove("nbf"); //not before time
                    options.ClaimActions.DeleteClaim("sid");//session id
                    options.ClaimActions.DeleteClaim("idp");//identity server
                    options.ClaimActions.DeleteClaim("s_hash");// state hash value
                    options.ClaimActions.DeleteClaim("auth_time");//authentication time
                    // options.ClaimActions.MapUniqueJsonKey("address", "address");
                    options.ClaimActions.MapUniqueJsonKey("role", "role");
                    options.ClaimActions.MapUniqueJsonKey("country", "country");
                    options.ClaimActions.MapUniqueJsonKey("subscriptionlevel", "subscriptionlevel");

                    options.SaveTokens = true;
                    options.ClientSecret = "secret";
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtClaimTypes.GivenName,
                        RoleClaimType = JwtClaimTypes.Role
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();
 
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
                // The default HSTS value is 30 days. You may want to change this for
                // production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Gallery}/{action=Index}/{id?}");
            });
        }
    }
}
