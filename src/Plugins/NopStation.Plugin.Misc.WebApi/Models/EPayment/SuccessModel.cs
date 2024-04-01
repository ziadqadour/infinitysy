using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopStation.Plugin.Misc.WebApi.Models.EPayment
{
    public class SuccessModel
    {
        public string TransactionStat { get; set; } 
        public string IdTransaction { get; set; }
        public string AuthorizationNumber { get; set; }
        public string Stan { get; set; }
    }
}
