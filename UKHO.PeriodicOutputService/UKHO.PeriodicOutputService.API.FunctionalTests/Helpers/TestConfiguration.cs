using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot ConfigurationRoot;
        public FleetManagerB2BApiConfiguration fleetManagerB2BConfig = new FleetManagerB2BApiConfiguration();
        public class FleetManagerB2BApiConfiguration
        {
            public string userName { get; set; }
            public string password { get; set; }

            public string baseUrl { get; set; }

            public string subscriptionKey { get; set; }

            public string jwtAuthUnpEndpoint { get; set; }

            public string jwtAuthJwtEndpoint { get; set; }

            public string catalogueEndpoint { get; set; }

            public string invalidjwttoken { get; set; }

            public string nulljwttoken { get; set; }

            public string invalidsubscriptionkey { get; set; }

            public string nullsubscriptionkey { get; set; }

        }

        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json", false)
                               .Build();

            ConfigurationRoot.Bind("FleetManagerB2BApiConfiguration", fleetManagerB2BConfig);
        }

    }
}
