namespace UKHO.PeriodicOutputService.Common.Configuration
{
    public class BessStorageConfiguration
    {
        public string ConnectionString { get; set; }

        public string ContainerName { get; set; }

        public string QueueName { get; set; }

        public string SerialFileName { get; set; }

        public string ProductFileName { get; set; }

        public string ExchangeSetFolder { get; set; }

        public string EncRoot { get; set; }

        public string Info { get; set; }
    }
}

