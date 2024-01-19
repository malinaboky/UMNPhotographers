using Microsoft.EntityFrameworkCore;
using UMNPhotographers.Distribution.Domain;
using UMNPhotographers.Distribution.Services;


namespace UMNPhotographers.Distribution
{
     public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureDataBaseContext(services);
            ConfigureCustomServices(services);
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DataContext context)
        {    
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        
        private void ConfigureDataBaseContext(IServiceCollection services)
        {
            var postgresConnectionString = Configuration.GetConnectionString($"DefaultConnection");
            services.AddDbContext<DataContext>(options => options.UseNpgsql(postgresConnectionString));
        }

        private void ConfigureCustomServices(IServiceCollection services)
        {
            services.AddTransient<IDistributionService, DistributionService>();
            services.AddTransient<IParseService, ParseService>();
            services.AddTransient<IMessageService, MessageService>();
        }
    }
}
