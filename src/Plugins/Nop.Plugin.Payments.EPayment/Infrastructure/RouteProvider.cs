using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.EPayment.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //PDT
            //endpointRouteBuilder.MapControllerRoute("Plugin.EPayment.PDTHandler", "Plugins/EPayment/PDTHandler",
             //    new { controller = "EPayment", action = "PDTHandler" });

            //IPN
            //endpointRouteBuilder.MapControllerRoute("Plugin.EPayment.IPNHandler", "Plugins/EPayment/IPNHandler",
              //   new { controller = "PaymentPayPalStandardIpn", action = "IPNHandler" });

            //Cancel
            endpointRouteBuilder.MapControllerRoute("Plugin.EPayments.CancelOrder", "Plugins/EPayment/CancelOrder",
                 new { controller = "EPayment", action = "CancelOrder" });

            endpointRouteBuilder.MapControllerRoute("Plugin.EPayments.RedirectToBank", "Plugins/EPayment/RedirectToBank",
                 new { controller = "EPayment", action = "RedirectToBank" });

            endpointRouteBuilder.MapControllerRoute("Plugin.EPayments.Success", "Plugins/EPayment/Success",
                 new { controller = "EPayment", action = "Success" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => -1;
    }
}