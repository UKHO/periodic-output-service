namespace UKHO.PeriodicOutputService.Common.Configuration
{
    public class BessStorageConfiguration
    {
        public string ConnectionString { get; set; }

        public string ContainerName { get; set; }

        public string QueueName { get; set; }

        public string MessageContainerName { get; set; }
    }
}

