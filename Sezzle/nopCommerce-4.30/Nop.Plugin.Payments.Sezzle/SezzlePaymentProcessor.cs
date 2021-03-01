using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Payments.Sezzle.Payload;
using Nop.Plugin.Payments.Sezzle.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Tax;

namespace Nop.Plugin.Payments.Sezzle
{
    /// <summary>
    /// Sezzlement processor
    /// </summary>
    public class SezzlePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly SezzleHttpClient _sezzleHttpClient;
        private readonly SezzlePaymentSettings _sezzlePaymentSettings;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;

        #endregion

        #region Ctor

        public SezzlePaymentProcessor(CurrencySettings currencySettings,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IPaymentService paymentService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            SezzleHttpClient sezzleHttpClient,
            SezzlePaymentSettings sezzlePaymentSettings,
            IOrderTotalCalculationService orderTotalCalculationService)
        {
            _currencySettings = currencySettings;
            _checkoutAttributeParser = checkoutAttributeParser;
            _currencyService = currencyService;
            _genericAttributeService = genericAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _paymentService = paymentService;
            _settingService = settingService;
            _taxService = taxService;
            _webHelper = webHelper;
            _sezzleHttpClient = sezzleHttpClient;
            _sezzlePaymentSettings = sezzlePaymentSettings;
            _orderTotalCalculationService = orderTotalCalculationService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var checkoutUrl = _sezzleHttpClient.GetCheckoutUrl(postProcessPaymentRequest).GetAwaiter().GetResult();
            _httpContextAccessor.HttpContext.Response.Redirect(checkoutUrl);
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            
            var orderTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart);

            if (_sezzlePaymentSettings.MinCheckoutAmount > 0)
            {
                if (orderTotal < _sezzlePaymentSettings.MinCheckoutAmount)
                {
                    return true;
                }
            }
            else if (String.IsNullOrEmpty(_sezzlePaymentSettings.PublicKey)
            || String.IsNullOrEmpty(_sezzlePaymentSettings.PrivateKey))
            {
                return true;
            }

            return false;
            
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            try
            {
                if (capturePaymentRequest == null)
                    throw new ArgumentException(nameof(capturePaymentRequest));

                //capture transaction
                var captureStatus = _sezzleHttpClient.CapturePayment(capturePaymentRequest.Order.OrderGuid.ToString())
                    .GetAwaiter()
                    .GetResult();
                if (!captureStatus)
                {
                    throw new NopException("Something went while capturing.");
                }

                //successfully captured
                return new CapturePaymentResult
                {
                    NewPaymentStatus = PaymentStatus.Paid,
                    CaptureTransactionId = capturePaymentRequest.Order.OrderGuid.ToString(),
                    CaptureTransactionResult = "Payment has been captured successfully"
                };
            }
            catch (Exception e)
            {
                return new CapturePaymentResult { Errors = new[] { e.Message } };
            }
            
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            try
            {
                if (refundPaymentRequest == null)
                    throw new ArgumentException(nameof(refundPaymentRequest));

                //refund transaction
                var status = _sezzleHttpClient.RefundPayment(
                    refundPaymentRequest.Order.OrderGuid.ToString(),
                    refundPaymentRequest.AmountToRefund
                    )
                    .GetAwaiter()
                    .GetResult();
                if (!status)
                    throw new NopException("Error occured while refunding amount.");

                //successfully refunded
                return new RefundPaymentResult
                {
                    NewPaymentStatus = PaymentStatus.Refunded
                };
            }
            catch (Exception e)
            {
                return new RefundPaymentResult { Errors = new[] { e.Message } };
            }
            
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentSezzle/Configure";
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "PaymentSezzle";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new SezzlePaymentSettings
            {
                UseSandbox = true,
                TransactionMode = TransactionMode.AuthorizeAndCapture
            });

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.MerchantId", "Merchant Id");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.MerchantId.Hint", "Specify your merchant id.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.PublicKey", "Public Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.PublicKey.Hint", "Specify your public key.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.PrivateKey", "Private Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.PrivateKey.Hint", "Specify your private key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.MinCheckoutAmount", "Minimum Checkout Amount");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.MinCheckoutAmount.Hint", "Specify minimum checkout amount to show the payment gateway.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.RedirectionTip", "You will be redirected to Sezzle Checkout to complete the order.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.UseSandbox", "Use Sandbox");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Instructions", @"
            <p>
	            <br />1. Click <a href=""https://dashboard.sezzle.in/merchant/signup"" target=""_blank"">here</a> to create your account if not done already.
	            <br />2. Note down the Merchant Id, Public Key and Private Key.
	            <br />3. Provide the Merchant Id, Public Key, Private Key, Transaction Mode in the form below.
                <br />4. Provide Minimum Checkout Amount to restrict Sezzle based on the value you provided(Optional).
	            <br />5. Click Save.
	            <br />
            </p>");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.PaymentMethodDescription", "Buy Now, Pay Later with 0% interest");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.TransactionMode", "Transaction Mode");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Sezzle.Fields.TransactionMode.Hint", "Specify transaction mode.");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<SezzlePaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.MerchantId");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.MerchantId.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.PublicKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.PublicKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.PrivateKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.PrivateKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.MinCheckoutAmount");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.MinCheckoutAmount.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.RedirectionTip");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.UseSandbox");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Fields.UseSandbox.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.Instructions");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Sezzle.PaymentMethodDescription");

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => true;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => false;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => true;

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

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription => _localizationService.GetResource("Plugins.Payments.Sezzle.PaymentMethodDescription");

        #endregion
    }
}