using System;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace AuroraPOS
{
    public class AppConfiguration
    {
        public readonly string _connectionString = string.Empty;
        public readonly IConfiguration Configuration;

        public AppConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            configurationBuilder.AddJsonFile(path, false);

            Configuration = configurationBuilder.Build();
        }
        
        public static string GetLLaveConfig(string LLave)
        {
            var obj = new AppConfiguration();
            return obj.Configuration[LLave];
        }

        public static IConfiguration Get()
        {
            var obj = new AppConfiguration();
            return obj.Configuration;
        }        

    }
}
