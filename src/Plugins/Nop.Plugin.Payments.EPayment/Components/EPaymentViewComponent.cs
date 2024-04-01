using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.EPayment.Components
{
    public class EPaymentViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.EPayment/Views/PaymentInfo.cshtml");
        }
    }
}
