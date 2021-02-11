using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Sezzle.Models;
using Nop.Plugin.Payments.Sezzle.Payload;
using Nop.Plugin.Payments.Sezzle.Services;
using Nop.Services;
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

namespace Nop.Plugin.Payments.Sezzle.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class PaymentSezzleController : BasePaymentController
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
        private readonly SezzlePaymentSettings _sezzlePaymentSettings;
        private readonly SezzleHttpClient _sezzleHttpClient;

        #endregion

        #region Ctor

        public PaymentSezzleController(IGenericAttributeService genericAttributeService,
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
            ShoppingCartSettings shoppingCartSettings,
            SezzlePaymentSettings sezzlePaymentSettings,
            SezzleHttpClient sezzleHttpClient)
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
            _sezzlePaymentSettings = sezzlePaymentSettings;
            _sezzleHttpClient = sezzleHttpClient;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Process payment
        /// </summary>
        /// <param name="orderNumber">Order number</param>
        /// <returns>Returns true for success else false</returns>
        protected virtual bool ProcessPayment(string orderNumber)
        {
            Guid orderNumberGuid;

            try
            {
                orderNumberGuid = new Guid(orderNumber);
            }
            catch
            {
                orderNumberGuid = Guid.Empty;
            }

            var order = _orderService.GetOrderByGuid(orderNumberGuid);
            var sezzleOrder = _sezzleHttpClient.GetOrderFromSezzle(orderNumber).GetAwaiter().GetResult();
            
            if (sezzleOrder == null)
            {
                var note = $"Sezzle Order is not found. Sezzle Order Reference #{orderNumber}";
                _logger.Error(note, new NopException(orderNumber));

                _orderService.InsertOrderNote(new OrderNote
                {
                    OrderId = order.Id,
                    Note = note,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                _orderService.UpdateOrder(order);
                return false;
            }

            DateTime captureExpirationInUtc = Convert.ToDateTime(sezzleOrder.CaptureExpiration).ToUniversalTime();
            var (isValid, errorMsg) = ValidateCaptureExpiration(sezzleOrder, captureExpirationInUtc);
            if (!isValid && !String.IsNullOrEmpty(errorMsg))
            {
                _logger.Error(errorMsg, new NopException(orderNumber));

                _orderService.InsertOrderNote(new OrderNote
                {
                    OrderId = order.Id,
                    Note = errorMsg,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                _orderService.UpdateOrder(order);
                return false;
            }


            //validate order total
            if (!(sezzleOrder.AmountInCents).Equals((int)Math.Round(order.OrderTotal * 100, 2)))
            {
                var errorStr = $"Sezzle Returned order total {sezzleOrder.AmountInCents} doesn't equal order total {order.OrderTotal}. Order# {order.Id}.";
                //log
                _logger.Error(errorStr);
                //order note
                _orderService.InsertOrderNote(new OrderNote
                {
                    OrderId = order.Id,
                    Note = errorStr,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                _orderService.UpdateOrder(order);

                return false;
            }

            switch (_sezzlePaymentSettings.TransactionMode)
            {
                case TransactionMode.AuthorizeOnly:
                    if (_orderProcessingService.CanMarkOrderAsAuthorized(order))
                    {
                        order.AuthorizationTransactionId = orderNumber;
                        order.AuthorizationTransactionResult = "Authorized";
                        _orderProcessingService.MarkAsAuthorized(order);

                        _orderService.InsertOrderNote(new OrderNote
                        {
                            OrderId = order.Id,
                            Note = $"Capture the payment before {captureExpirationInUtc}",
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        _orderService.UpdateOrder(order);
                        return true;
                    }
                    break;
                case TransactionMode.AuthorizeAndCapture:
                    if (String.IsNullOrEmpty(sezzleOrder.CapturedAt) && _orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        var captureStatus = _sezzleHttpClient.CapturePayment(order.OrderGuid.ToString())
                        .GetAwaiter()
                        .GetResult();

                        if (!captureStatus)
                        {
                            _orderService.InsertOrderNote(new OrderNote
                            {
                                OrderId = order.Id,
                                Note = $"Something went wrong while capturing",
                                DisplayToCustomer = false,
                                CreatedOnUtc = DateTime.UtcNow
                            });
                            return false;
                        }

                        _orderService.InsertOrderNote(new OrderNote
                        {
                            OrderId = order.Id,
                            Note = $"Payment captured by Sezzle",
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        order.CaptureTransactionId = orderNumber;
                        order.CaptureTransactionResult = "Captured";
                        _orderService.UpdateOrder(order);
                        _orderProcessingService.MarkOrderAsPaid(order);
                        return true;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// Validate capture expiration
        /// </summary>
        /// <param name="sezzleOrder">Sezzle Order</param>
        /// <param name="captureExpirationInUtc">Sezzle capture expiration</param>
        /// <returns>Returns true for success else false with error string</returns>
        public (bool,string) ValidateCaptureExpiration(ObtainOrderResponse sezzleOrder, DateTime captureExpirationInUtc)
        {
            var errorStr = string.Empty;
            if (String.IsNullOrEmpty(sezzleOrder.CaptureExpiration))
            {
                errorStr = "Order not authorized at Sezzle";
                return (false, errorStr);
            }
            else if (DateTime.UtcNow.CompareTo(captureExpirationInUtc) > 0)
            {
                errorStr = "Capture time has been expired for this order";
                return (false, errorStr);
            }
            return (true,errorStr);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Configuration view settings
        /// </summary>
        /// <returns>Returns to the config view page</returns>
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var sezzlePaymentSettings = _settingService.LoadSetting<SezzlePaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = sezzlePaymentSettings.UseSandbox,
                MerchantId = sezzlePaymentSettings.MerchantId,
                PublicKey = sezzlePaymentSettings.PublicKey,
                PrivateKey = sezzlePaymentSettings.PrivateKey,
                ActiveStoreScopeConfiguration = storeScope,
                TransactionModeId = (int)sezzlePaymentSettings.TransactionMode,
                TransactionModeValues = sezzlePaymentSettings.TransactionMode.ToSelectList(),
                MinCheckoutAmount = sezzlePaymentSettings.MinCheckoutAmount

            };

            if (storeScope <= 0)
                return View("~/Plugins/Payments.Sezzle/Views/Configure.cshtml", model);

            model.UseSandbox_OverrideForStore = _settingService.SettingExists(sezzlePaymentSettings, x => x.UseSandbox, storeScope);
            model.MerchantId_OverrideForStore = _settingService.SettingExists(sezzlePaymentSettings, x => x.MerchantId, storeScope);
            model.PublicKey_OverrideForStore = _settingService.SettingExists(sezzlePaymentSettings, x => x.PublicKey, storeScope);
            model.PrivateKey_OverrideForStore = _settingService.SettingExists(sezzlePaymentSettings, x => x.PrivateKey, storeScope);
            model.TransactionModeId_OverrideForStore = _settingService.SettingExists(sezzlePaymentSettings, x => x.TransactionMode, storeScope);
            model.MinCheckoutAmount_OverrideForStore = _settingService.SettingExists(sezzlePaymentSettings, x => x.MinCheckoutAmount, storeScope);

            return View("~/Plugins/Payments.Sezzle/Views/Configure.cshtml", model);
        }

        /// <summary>
        /// Admin payment configuration
        /// </summary>
        /// <param name="model">Configuration model</param>
        /// <returns>Return the configuration settings</returns>
        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var sezzlePaymentSettings = _settingService.LoadSetting<SezzlePaymentSettings>(storeScope);

            //save settings
            sezzlePaymentSettings.UseSandbox = model.UseSandbox;
            sezzlePaymentSettings.MerchantId = model.MerchantId;
            sezzlePaymentSettings.PublicKey = model.PublicKey;
            sezzlePaymentSettings.PrivateKey = model.PrivateKey;
            sezzlePaymentSettings.TransactionMode = (TransactionMode)model.TransactionModeId;
            sezzlePaymentSettings.MinCheckoutAmount = model.MinCheckoutAmount;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(sezzlePaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(sezzlePaymentSettings, x => x.MerchantId, model.MerchantId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(sezzlePaymentSettings, x => x.PublicKey, model.PublicKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(sezzlePaymentSettings, x => x.PrivateKey, model.PrivateKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(sezzlePaymentSettings, x => x.TransactionMode, model.TransactionModeId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(sezzlePaymentSettings, x => x.MinCheckoutAmount, model.MinCheckoutAmount_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        /// <summary>
        /// Complete Order
        /// </summary>
        /// <returns>Returns to order details page or order completion page</returns>
        public IActionResult CompleteOrder()
        {
            var orderReferenceId = _webHelper.QueryString<string>("reference_id");

            if (!(_paymentPluginManager.LoadPluginBySystemName("Payments.Sezzle") is SezzlePaymentProcessor processor) || !_paymentPluginManager.IsPluginActive(processor))
                throw new NopException("Sezzle Standard module cannot be loaded");

            Guid orderNumberGuid = Guid.Empty;
            try
            {
                orderNumberGuid = new Guid(orderReferenceId);
            }
            catch
            {
                return RedirectToAction("Index", "Home", new { area = string.Empty });
            }

            var order = _orderService.GetOrderByGuid(orderNumberGuid);

            if (order == null)
            {
                return RedirectToAction("Index", "Home", new { area = string.Empty });
            }

            var status = ProcessPayment(orderReferenceId);

            if (!status)
            {
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });
            }

            return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
        }

        /// <summary>
        /// Cancel Order
        /// </summary>
        /// <returns>Return to either homepage or order details page</returns>
        public IActionResult CancelOrder()
        {
            var orderReferenceId = _webHelper.QueryString<string>("reference_id");

            if (!(_paymentPluginManager.LoadPluginBySystemName("Payments.Sezzle") is SezzlePaymentProcessor processor) || !_paymentPluginManager.IsPluginActive(processor))
                throw new NopException("Sezzle Standard module cannot be loaded");

            Guid orderNumberGuid = Guid.Empty;
            try
            {
                orderNumberGuid = new Guid(orderReferenceId);
            }
            catch
            {
                return RedirectToAction("Index", "Home", new { area = string.Empty });
            }

            var order = _orderService.GetOrderByGuid(orderNumberGuid);

            if (order == null)
            {
                return RedirectToAction("Index", "Home", new { area = string.Empty });
            }
            else if (_orderProcessingService.CanCancelOrder(order))
            {
                _orderProcessingService.CancelOrder(order, true);
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });
            }
            
            return RedirectToAction("Index", "Home", new { area = string.Empty });
        }

        #endregion
    }
}