using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Controllers;
using UKHO.FmEssFssMock.API.Services;

namespace UKHO.FmEssFssMock.API
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
            services.AddControllers();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.Configure<FleetManagerApiConfiguration>(Configuration.GetSection("FleetManagerB2BApiConfiguration"));
            services.Configure<FileDirectoryPathConfiguration>(Configuration.GetSection("FileDirectoryPath"));
            services.AddScoped<ExchangeSetService>();
            services.AddScoped<FleetManagerController>();
            services.Configure<ExchangeSetConfiguration>(Configuration.GetSection("ExchangeSetService"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
 {
     endpoints.MapControllers();
 });
        }
    }
}
