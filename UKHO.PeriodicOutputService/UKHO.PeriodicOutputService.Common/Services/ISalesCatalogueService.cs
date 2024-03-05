using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface ISalesCatalogueService
    {
        public Task<SalesCatalogueDataResponse> GetSalesCatalogueData();
    }
}
