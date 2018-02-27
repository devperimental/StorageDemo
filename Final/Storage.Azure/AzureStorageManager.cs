using CommonTypes.Behaviours;
using CommonTypes.Settings;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Polly;
using Storage.Behaviours;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Storage.Azure
{
    public class AzureStorageManager : IStorage
    {
        private AzureSettings _azureSettings;
        private IAppLogger _appLogger;
        private Policy _localRetryPolicy;

        public AzureStorageManager(IAppLogger appLogger, AzureSettings azureSettings)
        {
            _azureSettings = azureSettings;
            _appLogger = appLogger;

            _localRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        var msg = $"AzureStorageManager Retry - Count:{retryCount}, Exception:{exception.Message}";
                        _appLogger?.LogWarning(msg);
                    }
                );

        }

        private string GetConnectionString()
        {
            return string.Format(_azureSettings.Storage.BlobStorageUri, _azureSettings.Storage.StorageAccountName, _azureSettings.Storage.StoragePrimaryKey);
        }

        private CloudBlobClient GetBlobClient()
        {
            var cloudConnectionString = GetConnectionString();
            var storageAccount = CloudStorageAccount.Parse(cloudConnectionString);
            return storageAccount.CreateCloudBlobClient();
        }

        public async Task SaveFile(Stream stream, string containerName, string fileName, string contentType)
        {
            await _localRetryPolicy.ExecuteAsync(async () =>
            {
                var blobClient = GetBlobClient();
                var container = blobClient.GetContainerReference(containerName);
                var blockBlob = container.GetBlockBlobReference(fileName);

                blockBlob.Properties.ContentType = contentType;

                await blockBlob.UploadFromStreamAsync(stream);
                _appLogger.LogMessage($"Saved file to {containerName}/{fileName} of type {contentType}");
            });
        }

        public async Task DeleteFile(string containerName, string filePath)
        {
            await _localRetryPolicy.ExecuteAsync(async () =>
            {
                var blobClient = GetBlobClient();
                var container = blobClient.GetContainerReference(containerName);
                var blockBlob = container.GetBlockBlobReference(filePath);
                await blockBlob.DeleteIfExistsAsync();

                _appLogger.LogMessage($"Deleted object {containerName}/{filePath}");
            });
        }


        public async Task<MemoryStream> LoadStream(string containerName, string filePath)
        {
            var memStream = new MemoryStream();

            await _localRetryPolicy.ExecuteAsync(async () =>
            {
                var storageclient = GetBlobClient();
                var uri = new Uri(_azureSettings.Storage.StorageBaseUrl + "/" + containerName + "/" + filePath, UriKind.Absolute);
                var blob = await storageclient.GetBlobReferenceFromServerAsync(uri);

                if (await blob.ExistsAsync())
                {
                    await blob.DownloadToStreamAsync(memStream);
                    memStream.Position = 0;
                }

                _appLogger.LogMessage($"Loaded stream {containerName}/{filePath}");
            });

            return memStream;
        }

        public async Task<string> LoadFile(string container, string filePath)
        {
            var output = string.Empty;

            using (var memStream = await LoadStream(container, filePath))
            {
                using (var sr = new StreamReader(memStream))
                {
                    output = sr.ReadToEnd();
                }
            }

            return output;
        }

        public async Task<string> LoadBase64(string container, string filePath)
        {
            var output = string.Empty;

            using (var memStream = await LoadStream(container, filePath))
            {
                output = Convert.ToBase64String(memStream.ToArray());
            }

            return output;
        }

        public async Task MoveFile(string sourceContainer, string sourceFile, string targetContainer, string targetFile)
        {
            await _localRetryPolicy.ExecuteAsync(async () =>
            {
                var blobClient = GetBlobClient();

                var sourceUri = new Uri(_azureSettings.Storage.StorageBaseUrl + "/" + sourceContainer + "/" + sourceFile, UriKind.Absolute);
                var sourceBlob = await blobClient.GetBlobReferenceFromServerAsync(sourceUri);

                var destinationContainer = blobClient.GetContainerReference(targetContainer);
                var destinationBlob = destinationContainer.GetBlockBlobReference(targetFile);

                await destinationBlob.StartCopyAsync((CloudBlockBlob)sourceBlob);
                await sourceBlob.DeleteAsync();

                _appLogger.LogMessage($"Moved file from {sourceContainer}/{sourceFile} to {targetContainer}/{targetFile}");
            });
        }

        
    }
}
