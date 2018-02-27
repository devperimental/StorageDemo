using CommonTypes.Behaviours;
using CommonTypes.Settings;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Polly;
using Storage.Behaviours;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Storage.GCP
{
    public class GCPStorageManager : IStorage
    {
        private GCPSettings _gcpSettings;
        private IAppLogger _appLogger;

        private Policy _localRetryPolicy;

        public GCPStorageManager(IAppLogger appLogger, GCPSettings gcpSettings)
        {
            _gcpSettings = gcpSettings;
            _appLogger = appLogger;

            _localRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3,
                       retryAttempt => TimeSpan.FromMilliseconds(200),
                           (exception, timeSpan, retryCount, context) =>
                           {
                               var msg = $"GCPStorageManager Retry - Count:{retryCount}, Exception:{exception.Message}";
                               _appLogger.LogWarning(msg);
                           });


        }

        /// <summary>
        /// https://cloud.google.com/storage/docs/reference/libraries
        /// </summary>
        private StorageClient GetStorageClient()
        {
            var googleCredential = GoogleCredential.FromFile(_gcpSettings.Storage.JsonAuthPath);
           
            return StorageClient.Create(googleCredential);
        }

        public async Task SaveFile(Stream stream, string containerName, string fileName, string contentType)
        {
            await _localRetryPolicy.ExecuteAsync(async () =>
            {
                using (var client = GetStorageClient())
                {
                    var bucketName = _gcpSettings.Storage.PrimaryBucket;
                    var file = containerName + "/" + fileName;

                    await client.UploadObjectAsync(bucketName, file, contentType, stream);
                    _appLogger.LogMessage($"Saved file to {containerName}/{fileName} of type {contentType}");
                }
            });
        }

        public async Task DeleteFile(string containerName, string filePath)
        {
            await _localRetryPolicy.ExecuteAsync(async () =>
            {
                using (var client = GetStorageClient())
                {
                    var bucketName = _gcpSettings.Storage.PrimaryBucket;
                    var file = containerName + "/" + filePath;

                    await client.DeleteObjectAsync(bucketName, file);
                    _appLogger.LogMessage($"Deleted object {containerName}/{filePath}");
                }
            });
        }

        public async Task<MemoryStream> LoadStream(string containerName, string filePath)
        {
            var memStream = new MemoryStream();

            await _localRetryPolicy.ExecuteAsync(async () =>
            {
                using (var client = GetStorageClient())
                {
                    var bucketName = _gcpSettings.Storage.PrimaryBucket;
                    var file = containerName + "/" + filePath;

                    await client.DownloadObjectAsync(bucketName, file, memStream);
                    memStream.Position = 0;

                    _appLogger.LogMessage($"Loaded stream {containerName}/{filePath}");
                }
            });

            return memStream;
        }

        public async Task<string> LoadFile(string containerName, string filePath)
        {
            var output = string.Empty;

            using (var memStream = await LoadStream(containerName, filePath))
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

        public async Task MoveFile(string sourceContainer, string sourceFilePath, string targetContainer, string targetFilePath)
        {
            await _localRetryPolicy.ExecuteAsync(async () =>
            {
                using (var client = GetStorageClient())
                {
                    string sourceBucketName = _gcpSettings.Storage.PrimaryBucket;
                    string targetBucketName = _gcpSettings.Storage.PrimaryBucket;

                    var sourceFile = sourceContainer + "/" + sourceFilePath;
                    var targetFile = targetContainer + "/" + targetFilePath;

                    await client.CopyObjectAsync(sourceBucketName, sourceFile, targetBucketName, targetFile);
                    await client.DeleteObjectAsync(sourceBucketName, sourceFile);

                    _appLogger.LogMessage($"Moved file from {sourceContainer}/{sourceFile} to {targetContainer}/{targetFile}");
                }
            });
        }


    }
}
