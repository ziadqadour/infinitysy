using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.EPayment
{
    /// <summary>
    /// Represents settings of the Bank payment plugin
    /// </summary>
    public class EPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets a business email
        /// </summary>
        public string BusinessEmail { get; set; }

        /// <summary>
        /// Gets or sets PDT identity token
        /// </summary>
        public string PdtToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to pass info about purchased items to PayPal
        /// </summary>
        public bool PassProductNamesAndTotals { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
    }
}
