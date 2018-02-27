using CommonTypes.Behaviours;
using CommonTypes.Settings;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace TestHarness
{
    class Program
    {
        private static AWSSettings _awsSettings;
        private static AzureSettings _azureSettings;
        private static GCPSettings _gcpSettings;
        private static IAppLogger _consoleLogger;

        static void Main(string[] args)
        {
            _consoleLogger = new ConsoleLogger();

            _consoleLogger.LogMessage("Starting Test Harness!");

            try
            {
                InitConfiguration();
            }
            catch (Exception ex)
            {
                _consoleLogger.LogError(ex);
            }
            finally
            {
                _consoleLogger.LogMessage("End Test Harness!");
            }
            Console.ReadLine();
        }

        static void InitConfiguration()
        {
            _consoleLogger.LogMessage("Start Init Config");

            // Used to build key/value based configuration settings for use in an application
            // Note: AddJsonFile is an extension methods for adding JsonConfigurationProvider.
            var builder = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appSettings.json");

            // Builds an IConfiguration with keys and values from the set of sources
            var configuration = builder.Build();

            // Bind the respective section to the respective settings class 
            _awsSettings = configuration.GetSection("aws").Get<AWSSettings>();
            _azureSettings = configuration.GetSection("azure").Get<AzureSettings>();
            _gcpSettings = configuration.GetSection("gcp").Get<GCPSettings>();

            _consoleLogger.LogMessage("End Init Config");
        }
    }
}
