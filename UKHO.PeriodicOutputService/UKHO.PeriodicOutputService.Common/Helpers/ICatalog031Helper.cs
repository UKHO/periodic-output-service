using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface ICatalog031Helper
    {
        public void RemoveReadmeEntryAndUpdateCatalog(string catalogFilePath);
    }
}
