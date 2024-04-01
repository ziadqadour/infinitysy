using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Controllers;
using NopStation.Plugin.Misc.Core.Controllers;
using NopStation.Plugin.Misc.WebApi.Models.Tests;
using Nop.Services.Logging;
using DocumentFormat.OpenXml.EMMA;
using Nop.Core.Domain.Payments;
using NopStation.Plugin.Misc.WebApi.Models.EPayment;

namespace NopStation.Plugin.Misc.WebApi.Controllers
{
    [Route("api/EPayment")]
    public class EPaymentApiController : Controller
    {
        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region Ctor

        public EPaymentApiController(IGenericAttributeService genericAttributeService,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IPaymentPluginManager paymentPluginManager,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            ILogger logger,
            INotificationService notificationService,
            ISettingService settingService,
            IStoreContext storeContext,
            IWebHelper webHelper,
            IWorkContext workContext,
            ShoppingCartSettings shoppingCartSettings)
        {
            _genericAttributeService = genericAttributeService;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _paymentPluginManager = paymentPluginManager;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _logger = logger;
            _notificationService = notificationService;
            _settingService = settingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _workContext = workContext;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion
        [HttpPost("Success")]
        public virtual async Task<IActionResult> Success([FromBody] SuccessModel model)
        {
            try
            {
                await _logger.InformationAsync("Ziad_Success idTransaction=" + model.IdTransaction);
                await _logger.InformationAsync("Ziad_Success transactionStat=" + model.TransactionStat);
                var order = await _orderService.GetOrderByIdAsync(int.Parse(model.IdTransaction));

                if (model.TransactionStat.Equals("S")) //Success Response from Bank
                {
                    //Checking orderId and subtotal if the equal the saved values then weverything is fine else return KO to cancel the operation.
                    // var orderTotalSentToBank = await _genericAttributeService.GetAttributeAsync<decimal?>(order, EPaymentHelper.OrderTotalSentToBank);
                    order.PaymentStatus = PaymentStatus.Paid;
                    await _orderService.UpdateOrderAsync(order);

                    var response = new
                    {
                        responseCode = "OK"
                    };


                    // Return the JSON response
                    return Json(response);

                }
                else  //Failed Response from Bank
                {

                    var response = new
                    {
                        responseCode = "KO"
                    };

                    // Return the JSON response
                    return Json(response);

                }
            }
            catch (Exception ex)
            {


                var response = new
                {
                    responseCode = "KO"
                };

                // Return the JSON response
                return Json(response);

            }
            
        }
    }
}
