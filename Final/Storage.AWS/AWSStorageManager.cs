using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CommonTypes.Behaviours;
using CommonTypes.Settings;
using Polly;
using Storage.Behaviours;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Storage.AWS
{
    public class AWSStorageManager : IStorage
    {
        private AWSSettings _awsSettings;
        private IAppLogger _appLogger;

        private Policy _retryPolicy;

        public AWSStorageManager(IAppLogger appLogger, AWSSettings awsSettings)
        {
            _awsSettings = awsSettings;
            _appLogger = appLogger;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        var msg = $"AWSStorageManager Retry - Count:{retryCount}, Exception:{exception.Message}";
                        _appLogger?.LogWarning(msg);
                    }
                );

        }

        private AmazonS3Client GetStorageClient()
        {
            return new AmazonS3Client(_awsSettings.Storage.AccessKey, _awsSettings.Storage.SecretKey, RegionEndpoint.GetBySystemName(_awsSettings.Storage.Region));
        }

        public async Task SaveFile(Stream stream, string containerName, string fileName, string contentType)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using (var client = GetStorageClient())
                {
                    PutObjectRequest putRequest = new PutObjectRequest
                    {
                        BucketName = _awsSettings.Storage.PrimaryBucket,
                        Key = containerName + "/" + fileName,
                        InputStream = stream,
                        ContentType = contentType
                    };

                    await client.PutObjectAsync(putRequest);
                    _appLogger.LogMessage($"Saved file to {containerName}/{fileName} of type {contentType}");
                }
            });
        }

        public async Task DeleteFile(string containerName, string filePath)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using (var client = GetStorageClient())
                {
                    var deleteObjectRequest = new DeleteObjectRequest
                    {
                        BucketName = _awsSettings.Storage.PrimaryBucket,
                        Key = containerName + "/" + filePath
                    };

                    await client.DeleteObjectAsync(deleteObjectRequest);
                    _appLogger.LogMessage($"Deleted object {containerName}/{filePath}");
                }
            });
        }


        public async Task<MemoryStream> LoadStream(string containerName, string filePath)
        {
            var memStream = new MemoryStream();

            await _retryPolicy.ExecuteAsync(async () =>
            {
                using (var client = GetStorageClient())
                {
                    var request = new GetObjectRequest
                    {
                        BucketName = _awsSettings.Storage.PrimaryBucket,
                        Key = containerName + "/" + filePath
                    };

                    using (var response = await client.GetObjectAsync(request))
                    {
                        response.ResponseStream.CopyTo(memStream);
                        memStream.Position = 0;
                    }

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

        public async Task<string> LoadBase64(string containerName, string filePath)
        {
            var output = string.Empty;

            using (var memStream = await LoadStream(containerName, filePath))
            {
                output = Convert.ToBase64String(memStream.ToArray());
            }

            return output;
        }

        public async Task MoveFile(string sourceContainer, string sourceFile, string targetContainer, string targetFile)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using (var client = GetStorageClient())
                {
                    CopyObjectRequest request = new CopyObjectRequest()
                    {
                        SourceBucket = _awsSettings.Storage.PrimaryBucket,
                        SourceKey = sourceContainer + "/" + sourceFile,
                        DestinationBucket = _awsSettings.Storage.AlternateBucket,
                        DestinationKey = targetContainer + "/" + targetFile
                    };
                    await client.CopyObjectAsync(request);
                    await DeleteFile(sourceContainer, sourceFile);

                    _appLogger.LogMessage($"Moved file from {sourceContainer}/{sourceFile} to {targetContainer}/{targetFile}");
                }
            });
        }
    }
}