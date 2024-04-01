using System;
namespace Nop.Plugin.Payments.EPayment.Models
{

    public class BankModel
    {
        public string TransactionReference { get; set; }
        public string TransactionAmount { get; set; }
        public string CardHolderMailAddress { get; set; }
        public string CardHolderPhoneNumber { get; set; }
        public string CardHolderIPAddress { get; set; }
        public string PspId { get; set; }
        public string MpiId { get; set; }
        public string MerchantKitId { get; set; }
        public string CardAcceptor { get; set; }
        public string Mcc { get; set; }
        public string AuthenticationToken { get; set; }
        public string Currency { get; set; }
        public string TransactionTypeIndicator { get; set; }
        public string RedirectBackUrl { get; set; }
        public string CallBackUrl { get; set; }
        public string Language { get; set; }
        public string CountryCode { get; set; }
        public string DateTimeBuyer { get; set; }
        public string DateTimeSIC { get; set; }


        public BankModel()
        {
        }
    }
}

