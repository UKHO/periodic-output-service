using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Filters;
using UKHO.FmEssFssMock.API.HealthChecks;
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
            services.AddHeaderPropagation(options =>
            {
                options.Headers.Add(CorrelationIdMiddleware.XCorrelationIdHeaderKey);
            });
            services.AddControllers(o => o.InputFormatters.Insert(0, new BinaryRequestBodyFormatter()));

            services.Configure<FleetManagerB2BApiConfiguration>(Configuration.GetSection("FleetManagerB2BApiConfiguration"));            
            services.Configure<ExchangeSetServiceConfiguration>(Configuration.GetSection("ExchangeSetServiceConfiguration"));

            services.AddScoped<FileShareService>();
            services.AddScoped<ExchangeSetService>();

            services.AddHealthChecks()
                .AddCheck<FleetManagerStubHealthCheck>("FleetManagerStubHealthCheck");

            services.AddHttpClient();
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
                endpoints.MapHealthChecks("/health-check");
            });
        }
    }
}
