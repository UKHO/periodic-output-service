namespace UKHO.PKSWireMock.API
{
    public class App : IHostedService
    {
        private readonly IWireMockService service;

        public App(IWireMockService service)
        {
            this.service = service;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            service.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            service.Stop();
            return Task.CompletedTask;
        }
    }
}
