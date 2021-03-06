﻿using CommonTypes.Behaviours;
using CommonTypes.Settings;
using Storage.Behaviours;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Storage.AWS
{
    public class AWSStorageManager : IStorage
    {
        private IAppLogger _appLogger;
        private AWSSettings _awsSettings;
        public AWSStorageManager(IAppLogger appLogger, AWSSettings awsSettings)
        {
            _appLogger = appLogger;
            _awsSettings = awsSettings;
        }

        public Task DeleteFile(string container, string filePath)
        {
            throw new NotImplementedException();
        }

        public Task<string> LoadBase64(string container, string filePath)
        {
            throw new NotImplementedException();
        }

        public Task<string> LoadFile(string container, string filePath)
        {
            throw new NotImplementedException();
        }

        public Task<MemoryStream> LoadStream(string container, string filePath)
        {
            throw new NotImplementedException();
        }

        public Task MoveFile(string sourceContainer, string sourceFilePath, string targetContainer, string targetFilePath)
        {
            throw new NotImplementedException();
        }

        public Task SaveFile(Stream stream, string container, string filePath, string contentType)
        {
            throw new NotImplementedException();
        }
    }
}
