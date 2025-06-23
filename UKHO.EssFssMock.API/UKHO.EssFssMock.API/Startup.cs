using UKHO.EssFssMock.API.Common;
using UKHO.EssFssMock.API.Filters;
using UKHO.EssFssMock.API.Services;

namespace UKHO.EssFssMock.API
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

            services.Configure<ExchangeSetServiceConfiguration>(Configuration.GetSection("ExchangeSetServiceConfiguration"));
            services.Configure<FileShareServiceConfiguration>(Configuration.GetSection("FileShareServiceConfiguration"));
            services.Configure<BessStorageConfiguration>(Configuration.GetSection("BessStorageConfiguration"));
            services.Configure<SharedKeyConfiguration>(Configuration.GetSection("SharedKeyConfiguration"));
            services.Configure<SalesCatalogueConfiguration>(Configuration.GetSection("SalesCatalogue"));

            services.AddScoped<FileShareService>();
            services.AddScoped<ExchangeSetService>();
            services.AddScoped<MockService>();
            services.AddScoped<AzureStorageService>();
            services.AddScoped<SharedKeyAuthFilter>();
            services.AddScoped<SalesCatalogueService>();
            
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

            app.Use(async (context, next) =>
            {
                var endpoint = context.GetEndpoint();

                if (endpoint == null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
