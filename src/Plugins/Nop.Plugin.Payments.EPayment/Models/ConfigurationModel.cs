using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.EPayment.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.EPayments.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [DataType(DataType.EmailAddress)]
        [NopResourceDisplayName("Plugins.EPayments.Fields.BusinessEmail")]
        public string BusinessEmail { get; set; }
        public bool BusinessEmail_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.EPayments.Fields.PDTToken")]
        public string PdtToken { get; set; }
        public bool PdtToken_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.EPayments.Fields.PassProductNamesAndTotals")]
        public bool PassProductNamesAndTotals { get; set; }
        public bool PassProductNamesAndTotals_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.EPayments.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.EPayments.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }
    }
}