using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAuthTokenProvider
    {
        Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
