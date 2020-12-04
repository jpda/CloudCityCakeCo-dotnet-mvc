using System;
using System.Collections.Generic;
using CloudCityCakeCo.Data;
using CloudCityCakeCo.Data.Repositories;
using CloudCityCakeCo.Models.DTO;
using CloudCityCakeCo.Models.Entities;
using CloudCityCakeCo.Services.Implementations;
using CloudCityCakeCo.Services.Interfaces;
using CloudCityCakeCo.Services.NotificationRules;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace CloudCityCakeCo
{
    // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/mfa?view=aspnetcore-3.1
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
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
                // Handling SameSite cookie according to https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
                options.HandleSameSiteCookieCompatibility();
            });

            //services.AddAuthentication().AddMicrosoftIdentityWebApp(Configuration, configSectionName: "AzureAdB2C");

            var b2cConfig = Configuration.GetSection("AzureAdB2C");

            services.AddAuthentication().AddOpenIdConnect(x =>
            {
                x.ClientId = b2cConfig["ClientId"];
                x.ClientSecret = b2cConfig["ClientSecret"];
                x.Authority = $"{b2cConfig["Instance"]}{b2cConfig["Domain"]}/{b2cConfig["SignUpSignInPolicyId"]}/v2.0";
                x.CallbackPath = b2cConfig["CallbackPath"];
                x.TokenValidationParameters = new TokenValidationParameters()
                {
                    // b2c has two issuer options per policy
                    ValidIssuers = new List<string>() {
                            $"{b2cConfig["Instance"]}/{b2cConfig["TenantId"]}/v2.0/",
                            $"{b2cConfig["Instance"]}/tfp/{b2cConfig["TenantId"]}/{b2cConfig["SignUpSignInPolicyId"]}/v2.0/"
                        }
                };
                x.ResponseType = OpenIdConnectResponseType.Code;
                x.SaveTokens = true;
                x.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProvider = ctx =>
                    {
                        ctx.ProtocolMessage.Scope += " https://jpdab2c.onmicrosoft.com/cloudcity/sodumb";
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "displayName"
                };
            });

            services.Configure<SendGridAccount>(Configuration.GetSection("SendGridAccount"));
            services.Configure<TwilioAccount>(Configuration.GetSection("TwilioAccount"));
            services.Configure<VerifySettings>(Configuration.GetSection("VerifySettings"));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultSql")));

            services.AddIdentity<User, ApplicationRole>(
                   options => options.SignIn.RequireConfirmedAccount = false)
               .AddEntityFrameworkStores<ApplicationDbContext>()
               .AddDefaultTokenProviders();

            services.AddAuthorization(options =>
                options.AddPolicy("TwoFactorEnabled",
                    x => x.RequireClaim("AuthenticationMethodsUsed", "mfa")));

            // services.AddAuthorization(x =>
            // {
            //     x.FallbackPolicy = x.DefaultPolicy;
            // });

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICakeOrderRepository, CakeOrderRepository>();

            services.AddScoped<ICakeOrderService, CakeOrderService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IMessagingService, MessagingService>();
            //services.AddScoped<IVerifyService, VerifyService>();


            services.AddScoped<IStatusNotificationRule, CompletedNotificationRule>();
            services.AddScoped<IStatusNotificationRule, AcceptedNotificationRule>();
            services.AddScoped<INotificationHandler, NotificationHandler>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddRazorPagesOptions(options =>
                {
                    // options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                    // options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                });

            services.AddHttpClient();
            services.AddRazorPages();
            services.AddControllersWithViews().AddMicrosoftIdentityUI();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
