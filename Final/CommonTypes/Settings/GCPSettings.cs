using System;
using System.Collections.Generic;
using System.Text;

namespace CommonTypes.Settings
{
    public class GCPSettings
    {
        public StorageSettings Storage { get; set; }

        public class StorageSettings
        {
            public string ProjectId { get; set; }
            public string JsonAuthPath { get; set; }
            public string PrimaryBucket { get; set; }
            public string AlternateBucket { get; set; }
        }
    }
}
