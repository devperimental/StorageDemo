using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Behaviours
{
    public interface IStorage
    {
        Task SaveFile(Stream stream, string container, string filePath, string contentType);
        Task DeleteFile(string container, string filePath);
        Task<MemoryStream> LoadStream(string container, string filePath);
        Task<string> LoadFile(string container, string filePath);
        Task<string> LoadBase64(string container, string filePath);
        Task MoveFile(string sourceContainer, string sourceFilePath, string targetContainer, string targetFilePath);
        
    }
}
