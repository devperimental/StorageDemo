namespace CommonTypes.Settings
{
    public class AWSSettings
    {
        public StorageSettings Storage { get; set; }

        public class StorageSettings
        {
            public string User { get; set; }
            public string AccessKey { get; set; }
            public string SecretKey { get; set; }
            public string PrimaryBucket { get; set; }
            public string AlternateBucket { get; set; }
            public string Region { get; set; }

        }
    }

    
}
