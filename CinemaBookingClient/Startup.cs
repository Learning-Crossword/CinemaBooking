using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CinemaBookingClient.Data;
using CinemaBookingClient.Models;
using CinemaBookingClient.Services;

namespace CinemaBookingClient
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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            var provider = (DBProviderType) Configuration.GetValue<int>("CinemaDataBaseProvider");

            switch(provider)
            {
                case DBProviderType.EFProvider:
                    services.AddScoped<ICinemaDataService, SqlCinemaDataService>();
                    break;
                case DBProviderType.ADONetDisconnectedProvider:
                    services.AddScoped<ICinemaDataService, AdonetDisconnectedDataService>();
                    break;
                case DBProviderType.ADONetOpenconnectedProvider:
                    throw new NotImplementedException("No set connection string!");
                    //services.AddScoped<ICinemaDataService, SqlCinemaDataService>();
                    break;
                default:
                    services.AddScoped<ICinemaDataService, SqlCinemaDataService>();
                    break;
            }           


            if (Configuration.GetValue<bool>("CinemaSchemeRemote"))
                services.AddScoped<ICinemaSeatPlanWS, CinemaSeatPlanWS>();
            else
                services.AddScoped<ICinemaSeatPlanWS, CinemaSeatPlanFromFile>();
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
    public enum DBProviderType
    {
        EFProvider = 0,
        ADONetDisconnectedProvider = 1,
        ADONetOpenconnectedProvider = 2
    }
}
