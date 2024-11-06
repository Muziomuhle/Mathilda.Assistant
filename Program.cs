
namespace Mathilda
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange:false)
            .AddEnvironmentVariables()
            .Build();

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<IIcsReader, IcsReader>();
            builder.Services.AddTransient<IClockifyService, ClockifyService>();
            builder.Services.AddHttpClient<IClockifyClient, ClockifyClient>().ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(builder.Configuration.GetSection("Clockify:BaseUrl").Value);
                c.DefaultRequestHeaders.Add("x-api-key", builder.Configuration.GetSection("Clockify:ApiKey").Value);
            });
            builder.Services.AddTransient<ITicketService, JiraService>();

            var app = builder.Build();
            IConfiguration config = app.Configuration;

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
