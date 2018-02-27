using System;
using System.Collections.Generic;
using System.Text;

namespace CommonTypes.Settings
{
    public class AzureSettings
    {
        public StorageSettings Storage { get; set; }

        public class StorageSettings
        {
            public string BlobStorageUri { get; set; }
            public string StorageAccountName { get; set; }
            public string StorageAccountBaseUrl { get; set; }
            public string StoragePrimaryKey { get; set; }
            public string StorageBaseUrl { get; set; }
        }
    }
}
