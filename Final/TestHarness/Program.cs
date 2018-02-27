using CommonTypes.Behaviours;
using CommonTypes.Settings;
using Microsoft.Extensions.Configuration;
using Storage.AWS;
using Storage.Azure;
using Storage.Behaviours;
using Storage.GCP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
                TestStorageFunctionality();
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

        static void TestStorageFunctionality()
        {
            Console.WriteLine("Start TestStorageFunctionality");

            var storageManagers = new List<IStorage>
            {
                new AWSStorageManager(_consoleLogger, _awsSettings),
                new GCPStorageManager(_consoleLogger, _gcpSettings),
                new AzureStorageManager(_consoleLogger, _azureSettings),
            };

            try
            {
                storageManagers.ForEach(c => { TestStorageOperations(c); });
            }
            catch (Exception ex)
            {
                _consoleLogger.LogError(ex);
            }

            Console.WriteLine("End TestStorageFunctionality");
        }

        static void TestStorageOperations(IStorage storageManager)
        {
            var fileContents = "These are the contents of the test file";
            var containerName = "test-container";
            var alternateContainerName = "alternate-container";
            var fileName = "test.txt";
            var contentType = "txt/plain";

            var byteArray = Encoding.UTF8.GetBytes(fileContents);
            var base64 = Convert.ToBase64String(byteArray);

            _consoleLogger.LogMessage($"Calling operations for {storageManager.GetType()}");


            using (var stream = new MemoryStream(byteArray))
            {
                storageManager.SaveFile(stream, containerName, fileName, contentType).Wait();
            }

            using (var streamFromLoad = storageManager.LoadStream(containerName, fileName).Result)
            {
                using (var reader = new StreamReader(streamFromLoad))
                {
                    var returnedContents = reader.ReadToEnd();
                    _consoleLogger.LogMessage($"LoadStream is match: {fileContents == returnedContents}");
                }
            }

            var stringFromLoad = storageManager.LoadFile(containerName, fileName).Result;
            _consoleLogger.LogMessage($"LoadFile is match: {fileContents == stringFromLoad}");

            var stringFromBase64 = storageManager.LoadBase64(containerName, fileName).Result;
            _consoleLogger.LogMessage($"LoadBase64 is match: {base64 == stringFromBase64}");

            storageManager.MoveFile(containerName, fileName, alternateContainerName, fileName).Wait();

            storageManager.DeleteFile(alternateContainerName, fileName).Wait();

        }
    }
}