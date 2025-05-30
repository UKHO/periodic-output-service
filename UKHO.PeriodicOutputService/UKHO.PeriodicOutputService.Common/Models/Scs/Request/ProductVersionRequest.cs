using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.PeriodicOutputService.Common.Models.Scs.Request
{
    public class ProductVersionRequest
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public int? UpdateNumber { get; set; }
    }
}
