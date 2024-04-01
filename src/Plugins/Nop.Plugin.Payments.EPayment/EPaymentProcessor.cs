using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Payments.EPayment.Components;
using Nop.Plugin.Payments.EPayment.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Tax;

namespace Nop.Plugin.Payments.EPayment
{
    /// <summary>
    /// Bank payment processor
    /// </summary>
    public class EPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly IAddressService _addressService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly EPaymentHttpClient _ePaymentHttpClient;
        private readonly EPaymentSettings _ePaymentSettings;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public EPaymentProcessor(CurrencySettings currencySettings,
            IAddressService addressService,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICountryService countryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IOrderService orderService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IProductService productService,
            ISettingService settingService,
            IStateProvinceService stateProvinceService,
            ITaxService taxService,
            IWebHelper webHelper,
            EPaymentHttpClient payPalStandardHttpClient,
            EPaymentSettings payPalStandardPaymentSettings, ILogger logger)
        {
            _currencySettings = currencySettings;
            _addressService = addressService;
            _checkoutAttributeParser = checkoutAttributeParser;
            _countryService = countryService;
            _currencyService = currencyService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _orderService = orderService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _productService = productService;
            _settingService = settingService;
            _stateProvinceService = stateProvinceService;
            _taxService = taxService;
            _webHelper = webHelper;
            _ePaymentHttpClient = payPalStandardHttpClient;
            _ePaymentSettings = payPalStandardPaymentSettings;
            _logger = logger;
        }

        #endregion

        #region Utilities
        
        /// <summary>
        /// Create common query parameters for the request
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the created query parameters
        /// </returns>
        private async Task<IDictionary<string, string>> CreateQueryParametersAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {

            //get store location
            var storeLocation = _webHelper.GetStoreLocation();

            //choosing correct order address
            var orderAddress = await _addressService.GetAddressByIdAsync(
                (postProcessPaymentRequest.Order.PickupInStore ? postProcessPaymentRequest.Order.PickupAddressId : postProcessPaymentRequest.Order.ShippingAddressId) ?? 0);

            
            
            //create query parameters
            return new Dictionary<string, string>
            {
                ["pspId"]= "PSP_001",
                ["mpiId"] = "mpi-test",
                ["merchantKitId"]= "mki-test",
                ["cardAcceptor"]= "01085011",
                ["mcc"]="5399",
                ["authenticationToken"]= "0CC8EC405D7ED29CE0650250568EE9ED",
                ["currency"]="SYP",
                ["transactionTypeIndicator"]="SS",
                ["redirectBackURL"]="infinitysy.com",
                ["callBackURL"]= "infinitysy.com",// api to send the payment result to infinity sy
                ["language"]="ar",
                ["countryCode"] ="SYR",
                ["dateTimeBuyer"] = DateTime.Now.ToString(),
                ["dateTimeSIC"] = DateTime.Now.ToString(),

                //PayPal ID or an email address associated with your PayPal account
                //["business"] = _ePaymentSettings.BusinessEmail,

                ////the character set and character encoding
                //["charset"] = "utf-8",

                ////set return method to "2" (the customer redirected to the return URL by using the POST method, and all payment variables are included)
                //["rm"] = "2",

                //["bn"] = EPaymentHelper.NopCommercePartnerCode,
                //["currency_code"] = (await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode,

                ////order identifier
                //["invoice"] = postProcessPaymentRequest.Order.CustomOrderNumber,
                //["custom"] = postProcessPaymentRequest.Order.OrderGuid.ToString(),

                ////PDT, IPN and cancel URL
                ////["return"] = $"{storeLocation}Plugins/EPayment/PDTHandler",
                ////["notify_url"] = $"{storeLocation}Plugins/EPayment/IPNHandler",
                ////["cancel_return"] = $"{storeLocation}Plugins/EPayment/CancelOrder",

                ////shipping address, if exists
                //["no_shipping"] = postProcessPaymentRequest.Order.ShippingStatus == ShippingStatus.ShippingNotRequired ? "1" : "2",
                //["address_override"] = postProcessPaymentRequest.Order.ShippingStatus == ShippingStatus.ShippingNotRequired ? "0" : "1",
                //["first_name"] = orderAddress?.FirstName,
                //["last_name"] = orderAddress?.LastName,
                //["address1"] = orderAddress?.Address1,
                //["address2"] = orderAddress?.Address2,
                //["city"] = orderAddress?.City,
                //["state"] = (await _stateProvinceService.GetStateProvinceByAddressAsync(orderAddress))?.Abbreviation,
                //["country"] = (await _countryService.GetCountryByAddressAsync(orderAddress))?.TwoLetterIsoCode,
                //["zip"] = orderAddress?.ZipPostalCode,
                //["email"] = orderAddress?.Email
            };
        }

        /// <summary>
        /// Add order items to the request query parameters
        /// </summary>
        /// <param name="parameters">Query parameters</param>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task AddItemsParametersAsync(IDictionary<string, string> parameters, PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //upload order items

            var cartTotal = decimal.Zero;
            var roundedCartTotal = decimal.Zero;

            //adding transactionReference (order id)
            parameters.Add("transactionReference", postProcessPaymentRequest.Order.Id.ToString());

            //adding rounded order total
            var roundedOrderTotal = Math.Round(postProcessPaymentRequest.Order.OrderTotal, 2);
            parameters.Add("transactionAmount", roundedOrderTotal.ToString("0.00", CultureInfo.InvariantCulture));

            //----------------------------------------

            var customer = await _customerService.GetCustomerByIdAsync(postProcessPaymentRequest.Order.CustomerId);

            //adding cardHolder MailAddress & Phone
            parameters.Add("cardHolderMailAddress", customer.Email);
            parameters.Add("cardHolderPhoneNumber", customer.Phone);
            parameters.Add("cardHolderIPAddress", customer.LastIpAddress);

            //add checkout attributes as order items
            var checkoutAttributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(postProcessPaymentRequest.Order.CheckoutAttributesXml);

            await foreach (var (attribute, values) in checkoutAttributeValues)
            {
                await foreach (var attributeValue in values)
                {
                    var (attributePrice, _) = await _taxService.GetCheckoutAttributePriceAsync(attribute, attributeValue, false, customer);
                    var roundedAttributePrice = Math.Round(attributePrice, 2);

                    //add query parameters
                    if (attribute == null)
                        continue;

                    cartTotal += attributePrice;
                    roundedCartTotal += roundedAttributePrice;
                  
                }
            }

            //add shipping fee as a separate order item, if it has price
            var roundedShippingPrice = Math.Round(postProcessPaymentRequest.Order.OrderShippingExclTax, 2);
            if (roundedShippingPrice > decimal.Zero)
            {
                cartTotal += postProcessPaymentRequest.Order.OrderShippingExclTax;
                roundedCartTotal += roundedShippingPrice;
            }

            //add payment method additional fee as a separate order item, if it has price
            var roundedPaymentMethodPrice = Math.Round(postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax, 2);
            if (roundedPaymentMethodPrice > decimal.Zero)
            {
                cartTotal += postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax;
                roundedCartTotal += roundedPaymentMethodPrice;
            }

            //add tax as a separate order item, if it has positive amount
            var roundedTaxAmount = Math.Round(postProcessPaymentRequest.Order.OrderTax, 2);
            if (roundedTaxAmount > decimal.Zero)
            {
                cartTotal += postProcessPaymentRequest.Order.OrderTax;
                roundedCartTotal += roundedTaxAmount;
            }

            if (cartTotal > postProcessPaymentRequest.Order.OrderTotal)
            {
                //get the difference between what the order total is and what it should be and use that as the "discount"
                var discountTotal = Math.Round(cartTotal - postProcessPaymentRequest.Order.OrderTotal, 2);
                roundedCartTotal -= discountTotal;
                //gift card or rewarded point amount applied to cart in nopCommerce - shows in PayPal as "discount"
                parameters.Add("discount_amount_cart", discountTotal.ToString("0.00", CultureInfo.InvariantCulture));


                parameters.Add("transactionAmount", roundedCartTotal.ToString("0.00", CultureInfo.InvariantCulture));
            }
            //save order total that actually sent to PayPal (used for PDT order total validation)
            await _genericAttributeService.SaveAttributeAsync(postProcessPaymentRequest.Order, EPaymentHelper.OrderTotalSentToBank, roundedCartTotal);
        }

        /// <summary>
        /// Add order total to the request query parameters
        /// </summary>
        /// <param name="parameters">Query parameters</param>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task AddOrderTotalParametersAsync(IDictionary<string, string> parameters, PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //round order total
            var roundedOrderTotal = Math.Round(postProcessPaymentRequest.Order.OrderTotal, 2);

            parameters.Add("cmd", "_xclick");
            parameters.Add("item_name", $"Order Number {postProcessPaymentRequest.Order.CustomOrderNumber}");
            parameters.Add("amount", roundedOrderTotal.ToString("0.00", CultureInfo.InvariantCulture));

            //save order total that actually sent to PayPal (used for PDT order total validation)
            await _genericAttributeService.SaveAttributeAsync(postProcessPaymentRequest.Order, EPaymentHelper.OrderTotalSentToBank, roundedOrderTotal);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Verifies IPN
        /// </summary>
        /// <param name="formString">Form string</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result, Values
        /// </returns>
        public async Task<(bool result, Dictionary<string, string> values)> VerifyIpnAsync(string formString)
        {
            var response = WebUtility.UrlDecode(await _ePaymentHttpClient.VerifyIpnAsync(formString));
            var success = response.Trim().Equals("VERIFIED", StringComparison.OrdinalIgnoreCase);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in formString.Split('&'))
            {
                var line = l.Trim();
                var equalPox = line.IndexOf('=');
                if (equalPox >= 0)
                    values.Add(line[0..equalPox], line[(equalPox + 1)..]);
            }

            return (success, values);
        }

        /// <summary>
        /// Gets PDT details
        /// </summary>
        /// <param name="tx">TX</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result, Values, Response
        /// </returns>
        public async Task<(bool result, Dictionary<string, string> values, string response)> GetPdtDetailsAsync(string tx)
        {
            var response = WebUtility.UrlDecode(await _ePaymentHttpClient.GetPdtDetailsAsync(tx));

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool firstLine = true, success = false;
            foreach (var l in response.Split('\n'))
            {
                var line = l.Trim();
                if (firstLine)
                {
                    success = line.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
                    firstLine = false;
                }
                else
                {
                    var equalPox = line.IndexOf('=');
                    if (equalPox >= 0)
                        values.Add(line[0..equalPox], line[(equalPox + 1)..]);
                }
            }

            return (success, values, response);
        }


        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult());
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //var baseUrl = "https://tecom.albaraka.com.sy:8433/ss-ecom-merchant-kit/buyForm/completeTransaction";

            //add order items query parameters to the request
            var parameters = new Dictionary<string, string>();
            await AddItemsParametersAsync(parameters, postProcessPaymentRequest);

            //remove null values from parameters
            parameters = parameters.Where(parameter => !string.IsNullOrEmpty(parameter.Value))
                .ToDictionary(parameter => parameter.Key, parameter => parameter.Value);

            await _logger.InformationAsync("Ziad_"+parameters.ToString());

            //ensure redirect URL doesn't exceed 2K chars to avoid "too long URL" exception
            var redirectUrl = QueryHelpers.AddQueryString(_webHelper.GetStoreHost(true) + "EPayment/RedirectToBank", parameters);

            await _logger.InformationAsync("Ziad_" + redirectUrl);
            _httpContextAccessor.HttpContext.Response.Redirect(redirectUrl);
            return;
            

        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - hide; false - display.
        /// </returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the additional handling fee
        /// </returns>
        public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
                _ePaymentSettings.AdditionalFee, _ePaymentSettings.AdditionalFeePercentage);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the capture payment result
        /// </returns>
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            return Task.FromResult(new RefundPaymentResult { Errors = new[] { "Refund method not supported" } });
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of validating errors
        /// </returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the payment info holder
        /// </returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            return Task.FromResult(new ProcessPaymentRequest());
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/EPayment/Configure";
        }

        /// <summary>
        /// Gets a type of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component type</returns>
        public Type GetPublicViewComponent()
        {
            return typeof(EPaymentViewComponent);
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new EPaymentSettings
            {
                UseSandbox = true
            });

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.EPayment.Fields.AdditionalFee"] = "Additional fee",
                ["Plugins.EPayment.Fields.AdditionalFee.Hint"] = "Enter additional fee to charge your customers.",
                ["Plugins.EPayment.Fields.AdditionalFeePercentage"] = "Additional fee. Use percentage",
                ["Plugins.EPayment.Fields.AdditionalFeePercentage.Hint"] = "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
                ["Plugins.EPayment.Fields.BusinessEmail"] = "Business Email",
                ["Plugins.EPayment.Fields.BusinessEmail.Hint"] = "Specify your PayPal business email.",
                ["Plugins.EPayment.Fields.PassProductNamesAndTotals"] = "Pass product names and order totals to the Bank",
                ["Plugins.EPayment.Fields.PassProductNamesAndTotals.Hint"] = "Check if product names and order totals should be passed to the Bank.",
                ["Plugins.EPayment.Fields.PDTToken"] = "PDT Identity Token",
                ["Plugins.EPayment.Fields.PDTToken.Hint"] = "Specify PDT identity token",
                ["Plugins.EPayment.Fields.RedirectionTip"] = "You will be redirected to the Bank site to complete the order.",
                ["Plugins.EPayment.Fields.UseSandbox"] = "Use Sandbox",
                ["Plugins.EPayment.Fields.UseSandbox.Hint"] = "Check to enable Sandbox (testing environment).",
                ["Plugins.EPayment.Instructions"] = @"
                    <p>
	                    <b>If you're using this gateway ensure that your primary store currency is supported by PayPal.</b>
	                    <br />
	                    <br />To use PDT, you must activate PDT and Auto Return in your PayPal account profile. You must also acquire a PDT identity token, which is used in all PDT communication you send to PayPal. Follow these steps to configure your account for PDT:<br />
	                    <br />1. Log in to your PayPal account (click <a href=""https://www.paypal.com/us/webapps/mpp/referral/paypal-business-account2?partner_id=9JJPJNNPQ7PZ8"" target=""_blank"">here</a> to create your account).
	                    <br />2. Click on the Profile button.
	                    <br />3. Click on the <b>Account Settings</b> link.
	                    <br />4. Select the <b>Website payments</b> item on left panel.
	                    <br />5. Find <b>Website Preferences</b> and click on the <b>Update</b> link.
	                    <br />6. Under <b>Auto Return</b> for <b>Website payments preferences</b>, select the <b>On</b> radio button.
	                    <br />7. For the <b>Return URL</b>, enter and save the URL on your site that will receive the transaction ID posted by PayPal after a customer payment (<em>{0}</em>).
                        <br />8. Under <b>Payment Data Transfer</b>, select the <b>On</b> radio button and get your <b>Identity token</b>.
	                    <br />9. Enter <b>Identity token</b> in the field below on the plugin configuration page.
                        <br />10. Click <b>Save</b> button on this page.
	                    <br />
                    </p>",
                ["Plugins.EPayment.PaymentMethodDescription"] = "You will be redirected to the Bank site to complete the payment",
                ["Plugins.EPayment.RoundingWarning"] = "It looks like you have \"ShoppingCartSettings.RoundPricesDuringCalculation\" setting disabled. Keep in mind that this can lead to a discrepancy of the order total amount, as the AlBarakah Bank only rounds to two decimals.",

            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<EPaymentSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.EPayments");

            await base.UninstallAsync();
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.EPayment.PaymentMethodDescription");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => false;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => false;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => false;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        #endregion
    }
}