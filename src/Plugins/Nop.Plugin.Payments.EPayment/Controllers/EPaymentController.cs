using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.EPayment.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;

namespace Nop.Plugin.Payments.EPayment.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class EPaymentController : BasePaymentController
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

        public EPaymentController(IGenericAttributeService genericAttributeService,
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

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var payPalStandardPaymentSettings = await _settingService.LoadSettingAsync<EPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = payPalStandardPaymentSettings.UseSandbox,
                BusinessEmail = payPalStandardPaymentSettings.BusinessEmail,
                PdtToken = payPalStandardPaymentSettings.PdtToken,
                PassProductNamesAndTotals = payPalStandardPaymentSettings.PassProductNamesAndTotals,
                AdditionalFee = payPalStandardPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = payPalStandardPaymentSettings.AdditionalFeePercentage,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope <= 0)
                return View("~/Plugins/Payments.EPayment/Views/Configure.cshtml", model);

            model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(payPalStandardPaymentSettings, x => x.UseSandbox, storeScope);
            model.BusinessEmail_OverrideForStore = await _settingService.SettingExistsAsync(payPalStandardPaymentSettings, x => x.BusinessEmail, storeScope);
            model.PdtToken_OverrideForStore = await _settingService.SettingExistsAsync(payPalStandardPaymentSettings, x => x.PdtToken, storeScope);
            model.PassProductNamesAndTotals_OverrideForStore = await _settingService.SettingExistsAsync(payPalStandardPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);
            model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(payPalStandardPaymentSettings, x => x.AdditionalFee, storeScope);
            model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(payPalStandardPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            return View("~/Plugins/Payments.EPayment/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var payPalStandardPaymentSettings = await _settingService.LoadSettingAsync<EPaymentSettings>(storeScope);

            //save settings
            payPalStandardPaymentSettings.UseSandbox = model.UseSandbox;
            payPalStandardPaymentSettings.BusinessEmail = model.BusinessEmail;
            payPalStandardPaymentSettings.PdtToken = model.PdtToken;
            payPalStandardPaymentSettings.PassProductNamesAndTotals = model.PassProductNamesAndTotals;
            payPalStandardPaymentSettings.AdditionalFee = model.AdditionalFee;
            payPalStandardPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.BusinessEmail, model.BusinessEmail_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.PdtToken, model.PdtToken_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.PassProductNamesAndTotals, model.PassProductNamesAndTotals_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        //action displaying notification (warning) to a store owner about inaccurate PayPal rounding
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> RoundingWarning(bool passProductNamesAndTotals)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //prices and total aren't rounded, so display warning
            if (passProductNamesAndTotals && !_shoppingCartSettings.RoundPricesDuringCalculation)
                return Json(new { Result = await _localizationService.GetResourceAsync("Plugins.EPayments.RoundingWarning") });

            return Json(new { Result = string.Empty });
        }

        public async Task<IActionResult> PDTHandler()
        {
            var tx = _webHelper.QueryString<string>("tx");

            if (await _paymentPluginManager.LoadPluginBySystemNameAsync("Payments.EPayment") is not EPaymentProcessor processor || !_paymentPluginManager.IsPluginActive(processor))
                throw new NopException("EPayment module cannot be loaded");

            var (result, values, response) = await processor.GetPdtDetailsAsync(tx);

            if (result)
            {
                values.TryGetValue("custom", out var orderNumber);
                var orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(orderNumber);
                }
                catch
                {
                    // ignored
                }

                var order = await _orderService.GetOrderByGuidAsync(orderNumberGuid);

                if (order == null)
                    return RedirectToAction("Index", "Home", new { area = string.Empty });

                var mcGross = decimal.Zero;

                try
                {
                    mcGross = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
                }
                catch (Exception exc)
                {
                    await _logger.ErrorAsync("EPayment PDT. Error getting mc_gross", exc);
                }

                values.TryGetValue("payer_status", out var payerStatus);
                values.TryGetValue("payment_status", out var paymentStatus);
                values.TryGetValue("pending_reason", out var pendingReason);
                values.TryGetValue("mc_currency", out var mcCurrency);
                values.TryGetValue("txn_id", out var txnId);
                values.TryGetValue("payment_type", out var paymentType);
                values.TryGetValue("payer_id", out var payerId);
                values.TryGetValue("receiver_id", out var receiverId);
                values.TryGetValue("invoice", out var invoice);
                values.TryGetValue("mc_fee", out var mcFee);

                var sb = new StringBuilder();
                sb.AppendLine("PayPal PDT:");
                sb.AppendLine("mc_gross: " + mcGross);
                sb.AppendLine("Payer status: " + payerStatus);
                sb.AppendLine("Payment status: " + paymentStatus);
                sb.AppendLine("Pending reason: " + pendingReason);
                sb.AppendLine("mc_currency: " + mcCurrency);
                sb.AppendLine("txn_id: " + txnId);
                sb.AppendLine("payment_type: " + paymentType);
                sb.AppendLine("payer_id: " + payerId);
                sb.AppendLine("receiver_id: " + receiverId);
                sb.AppendLine("invoice: " + invoice);
                sb.AppendLine("mc_fee: " + mcFee);

                var newPaymentStatus = EPaymentHelper.GetPaymentStatus(paymentStatus, string.Empty);
                sb.AppendLine("New payment status: " + newPaymentStatus);

                //order note
                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = sb.ToString(),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                //validate order total
                var orderTotalSentToPayPal = await _genericAttributeService.GetAttributeAsync<decimal?>(order, EPaymentHelper.OrderTotalSentToBank);
                if (orderTotalSentToPayPal.HasValue && mcGross != orderTotalSentToPayPal.Value)
                {
                    var errorStr = $"Bank PDT. Returned order total {mcGross} doesn't equal order total {order.OrderTotal}. Order# {order.Id}.";
                    //log
                    await _logger.ErrorAsync(errorStr);
                    //order note
                    await _orderService.InsertOrderNoteAsync(new OrderNote
                    {
                        OrderId = order.Id,
                        Note = errorStr,
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });

                    return RedirectToAction("Index", "Home", new { area = string.Empty });
                }

                //clear attribute
                if (orderTotalSentToPayPal.HasValue)
                    await _genericAttributeService.SaveAttributeAsync<decimal?>(order, EPaymentHelper.OrderTotalSentToBank, null);

                if (newPaymentStatus != PaymentStatus.Paid)
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });

                if (!_orderProcessingService.CanMarkOrderAsPaid(order))
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });

                //mark order as paid
                order.AuthorizationTransactionId = txnId;
                await _orderService.UpdateOrderAsync(order);
                await _orderProcessingService.MarkOrderAsPaidAsync(order);

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
            else
            {
                if (!values.TryGetValue("custom", out var orderNumber))
                    orderNumber = _webHelper.QueryString<string>("cm");

                var orderNumberGuid = Guid.Empty;

                try
                {
                    orderNumberGuid = new Guid(orderNumber);
                }
                catch
                {
                    // ignored
                }

                var order = await _orderService.GetOrderByGuidAsync(orderNumberGuid);
                if (order == null)
                    return RedirectToAction("Index", "Home", new { area = string.Empty });

                //order note
                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "PayPal PDT failed. " + response,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
        }

        public async Task<IActionResult> CancelOrder()
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var customer = await _workContext.GetCurrentCustomerAsync();
            var order = (await _orderService.SearchOrdersAsync(store.Id,
                customerId: customer.Id, pageSize: 1)).FirstOrDefault();

            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("Homepage");
        }

        public async Task<IActionResult> RedirectToBank(string transactionReference, decimal transactionAmount, string cardHolderMailAddress, string cardHolderPhoneNumber, string cardHolderIPAddress)
        {

            //await _logger.InformationAsync("Ziad_" + transactionReference);
            //await _logger.InformationAsync("Ziad_" + transactionAmount);
            //await _logger.InformationAsync("Ziad_" + cardHolderMailAddress);
            //await _logger.InformationAsync("Ziad_" + cardHolderPhoneNumber);
            //await _logger.InformationAsync("Ziad_" + cardHolderIPAddress);

            var store = await _storeContext.GetCurrentStoreAsync();
            var customer = await _workContext.GetCurrentCustomerAsync();
            var order = (await _orderService.SearchOrdersAsync(store.Id,
                customerId: customer.Id, pageSize: 1)).FirstOrDefault();

            // Create a new HttpClient instance
            HttpClient client = new HttpClient();

            BankModel model = new BankModel()
            {
                TransactionReference = transactionReference,
                TransactionAmount = transactionAmount.ToString(),
                CardHolderMailAddress = cardHolderMailAddress,
                CardHolderPhoneNumber = cardHolderPhoneNumber,
                CardHolderIPAddress = cardHolderIPAddress,
                PspId = "PSP_001",
                MpiId = "mpi_live",
                MerchantKitId = "mki-live",
                CardAcceptor = "11124717",
                Mcc = "5818",
                AuthenticationToken = "1542F28E283D26D0E063016811AC2FF3",
                Currency = "SYP",
                TransactionTypeIndicator = "SS",
                RedirectBackUrl = "https://infinitysy.com",
                CallBackUrl = "https://infinitysy.com/api/EPayment/Success",// api to send the payment result to infinity sys
                Language = "ar",
                CountryCode = "SYR",
                DateTimeBuyer = DateTime.Now.ToString(),
                DateTimeSIC = DateTime.Now.ToString()
            };


            //var bankUrl = "https://tecom.albaraka.com.sy:8433/ss-ecom-merchant-kit/buyForm/completeTransaction";

            //// Serialize the object to JSON
            //string json = JsonConvert.SerializeObject(data);

            //// Set the content type header to application/json

            //client.DefaultRequestHeaders
            //  .Accept
            //  .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header



            //// Send the POST request
            //HttpResponseMessage response = await client.PostAsync(bankUrl, new StringContent(json, Encoding.UTF8, "application/json"));

            //// Handle the response
            //if (response.IsSuccessStatusCode)
            //{
            //    // The request was successful, process the response
            //    string responseContent = await response.Content.ReadAsStringAsync();
            //    await _logger.InformationAsync("Ziad_" + responseContent);
            //    return Ok();
            //}
            //else
            //{
            //    // The request failed, handle the error
            //    await _logger.ErrorAsync("Ziad_" + response.StatusCode);
            //    var error = new NopException ("Ziad Log: Failed to send post request to the Bank");
            //    return BadRequest(error);
            //}

            return View("~/Plugins/Payments.EPayment/Views/RedirectToBank.cshtml", model);
        }

        //public async Task<IActionResult> Success(string authorizationNumber, string stan, string idTransaction, string transactionStat)
        //{
        //    await _logger.InformationAsync("Ziad_authorizationNumber" + authorizationNumber);
        //    await _logger.InformationAsync("Ziad_stan" + stan);
        //    await _logger.InformationAsync("Ziad_idTransaction" + idTransaction);
        //    await _logger.InformationAsync("Ziad_transactionStat" + transactionStat);

        //    if (string.IsNullOrEmpty(idTransaction))
        //    {
        //        var errorStr = $"idTransaction is empty!.";
        //        await _logger.ErrorAsync(errorStr);
        //        return BadRequest(errorStr);
        //    }

        //    var order = await _orderService.GetOrderByIdAsync(int.Parse(idTransaction));

        //    //Check if the same order Id is exist.
        //    if (order == null)
        //    {
        //        await _logger.InformationAsync("Ziad_" + "Order is Null");

        //        var errorStr = $"Bank Validation. There is no order has the id# {idTransaction}.";
        //        //log
        //        await _logger.ErrorAsync(errorStr);
        //        //order note
        //        await _orderService.InsertOrderNoteAsync(new OrderNote
        //        {
        //            OrderId = order.Id,
        //            Note = errorStr,
        //            DisplayToCustomer = false,
        //            CreatedOnUtc = DateTime.UtcNow
        //        });

        //        return RedirectToAction("Index", "Home", new { area = string.Empty });
        //    }

        //    if (transactionStat.Equals("S")) //Success Response from Bank
        //    {
        //        //Checking orderId and subtotal if the equal the saved values then weverything is fine else return KO to cancel the operation.

        //        var orderTotalSentToBank = await _genericAttributeService.GetAttributeAsync<decimal?>(order, EPaymentHelper.OrderTotalSentToBank);

        //        order.PaymentStatus = PaymentStatus.Paid;
        //        await _orderService.UpdateOrderAsync(order);

        //        //return the response to the Bank                


        //        return Ok("OK");



        //    }
        //    else  //Failed Response from Bank
        //    {

        //        order.PaymentStatus = PaymentStatus.Pending;
        //        await _orderService.UpdateOrderAsync(order);


        //        return BadRequest("KO");

        //    }


        //}

        //[HttpPost]
        //public async Task<IActionResult> Success(string authorizationNumber, string stan, string idTransaction, string transactionStat)
        //{
        //    await _logger.InformationAsync("Ziad_Success idTransaction=" + idTransaction);
        //    await _logger.InformationAsync("Ziad_Success transactionStat=" + transactionStat);
        //    var order = await _orderService.GetOrderByIdAsync(int.Parse(idTransaction));

        //    if (transactionStat.Equals("S")) //Success Response from Bank
        //    {
        //        //Checking orderId and subtotal if the equal the saved values then weverything is fine else return KO to cancel the operation.
        //       // var orderTotalSentToBank = await _genericAttributeService.GetAttributeAsync<decimal?>(order, EPaymentHelper.OrderTotalSentToBank);
        //        order.PaymentStatus = PaymentStatus.Paid;
        //        await _orderService.UpdateOrderAsync(order);

        //        var response = new
        //        {
        //            responseCode = "OK"
        //        };


        //        // Return the JSON response
        //        return Json(response);

        //    }
        //    else  //Failed Response from Bank
        //    {

        //        var response = new
        //        {
        //            responseCode = "KO"
        //        };

        //        // Return the JSON response
        //        return Json(response);

        //    }
        //}

       

        #endregion
    }
}