using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.Sezzle.Payload;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Payments;
using Nop.Services.Orders;
using Nop.Services.Catalog;

namespace Nop.Plugin.Payments.Sezzle.Services
{
    /// <summary>
    /// Represents the HTTP client to request Sezzle services
    /// </summary>
    public partial class SezzleHttpClient
    {
        #region Fields

        private readonly HttpClient _httpClient;
        private readonly SezzlePaymentSettings _sezzlePaymentSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly ICurrencyService _currencyService;
        private readonly IWebHelper _webHelper;
        private readonly IGenericAttributeService _genericAttributeService;

        private static Random _random = new Random();

        private readonly IAddressService _addressService;

        private readonly IOrderService _orderService;

        private readonly IProductService _productService;

        private readonly IStateProvinceService _stateProvinceService;

        private readonly ICountryService _countryService;

        #endregion

        #region Ctor

        public SezzleHttpClient(HttpClient client,
            SezzlePaymentSettings sezzlePaymentSettings,
            CurrencySettings currencySettings,
            ICurrencyService currencyService,
            IWebHelper webHelper,
            IGenericAttributeService genericAttributeService,
            IAddressService addressService,
            IOrderService orderService,
            IProductService productService,
            IStateProvinceService stateProvinceService,
            ICountryService countryService)
        {
            //configure client
            client.Timeout = TimeSpan.FromMilliseconds(5000);
            client.DefaultRequestHeaders.Add(HeaderNames.Accept, MimeTypes.ApplicationJson);

            _httpClient = client;
            _sezzlePaymentSettings = sezzlePaymentSettings;
            _currencySettings = currencySettings;
            _currencyService = currencyService;
            _webHelper = webHelper;
            _genericAttributeService = genericAttributeService;
            _addressService = addressService;
            _orderService = orderService;
            _productService = productService;
            _stateProvinceService = stateProvinceService;
            _countryService = countryService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get Order from Sezzle
        /// </summary>
        /// <param name="referenceId">Order reference id</param>
        /// <returns>The asynchronous task whose result contains the order info</returns>
        public async Task<ObtainOrderResponse> GetOrderFromSezzle(string referenceId)
        {
            try
            {
                //get response
                var url = _sezzlePaymentSettings.UseSandbox ?
                    $"https://sandbox.gateway.sezzle.in/v1/orders/{referenceId}" :
                    $"https://gateway.sezzle.in/v1/orders/{referenceId}";
                // var requestContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, MimeTypes.ApplicationJson);
                var authToken = GetAuthToken().GetAwaiter().GetResult();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                var response = await _httpClient.GetAsync(url);

                //return received access token
                var responseContent = await response.Content.ReadAsStringAsync();
                var orderResponse = JsonConvert.DeserializeObject<ObtainOrderResponse>(responseContent);
                return orderResponse;
            }
            catch (AggregateException exception)
            {
                //rethrow actual exception
                throw exception.InnerException;
            }

        }

        /// <summary>
        /// Capture payment at Sezzle
        /// </summary>
        /// <param name="referenceId">Order reference id</param>
        /// <returns>The asynchronous task whose result contains status and message</returns>
        public async Task<bool> CapturePayment(string referenceId)
        {
            try
            {
                //get response
                var url = _sezzlePaymentSettings.UseSandbox ?
                    $"https://sandbox.gateway.sezzle.in/v1/checkouts/{referenceId}/complete" :
                    $"https://gateway.sezzle.in/v1/checkouts/{referenceId}/complete";
                // var requestContent = new StringContent(null, Encoding.UTF8, MimeTypes.ApplicationJson);
                var authToken = GetAuthToken().GetAwaiter().GetResult();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                var response = await _httpClient.PostAsync(url, null);

                return response.IsSuccessStatusCode;
            }
            catch (AggregateException exception)
            {
                //rethrow actual exception
                throw exception.InnerException;
            }

        }

        /// <summary>
        /// Refund Payment
        /// </summary>
        /// <param name="referenceId">Order reference id</param>
        /// <param name="amount">Refund amount</param>
        /// <returns>The asynchronous task whose result contains the status of api response</returns>
        public async Task<bool> RefundPayment(string referenceId, decimal amount)
        {
            try
            {
                //get response
                var url = _sezzlePaymentSettings.UseSandbox ?
                    $"https://sandbox.gateway.sezzle.in/v1/orders/{referenceId}/refund" :
                    $"https://gateway.sezzle.in/v1/orders/{referenceId}/refund";
                
                var authToken = GetAuthToken().GetAwaiter().GetResult();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                var request = new RefundPayload
                {
                    Amount = new PricePayload
                    {
                        AmountInCents = (long)Math.Round((amount * 100), 2),
                        Currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode
                    }
                };
                var requestContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, MimeTypes.ApplicationJson);
                var response = await _httpClient.PostAsync(url, requestContent);

                //return received access token
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode ? true : false;
            }
            catch (AggregateException exception)
            {
                //rethrow actual exception
                throw exception.InnerException;
            }

        }

        /// <summary>
        /// Get auth token 
        /// </summary>
        /// <returns>The asynchronous task whose result contains the auth information</returns>
        public async Task<string> GetAuthToken()
        {
            try
            {
                //get response
                var url = _sezzlePaymentSettings.UseSandbox ?
                    "https://sandbox.gateway.sezzle.in/v1/authentication" :
                    "https://gateway.sezzle.in/v1/authentication";
                var request = new ObtainAuthTokenRequest
                {
                    PublicKey = _sezzlePaymentSettings.PublicKey,
                    PrivateKey = _sezzlePaymentSettings.PrivateKey
                };
                var requestContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, MimeTypes.ApplicationJson);
                var response = await _httpClient.PostAsync(url, requestContent);

                //return received access token
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonConvert.DeserializeObject<ObtainAuthTokenResponse>(responseContent);
                return authResponse?.Token;
            }
            catch (AggregateException exception)
            {
                //rethrow actual exception
                throw exception.InnerException;
            }

        }

        /// <summary>
        /// Get Checkout Request
        /// </summary>
        /// <param name="postProcessPaymentRequest">Order reference id</param>
        /// <returns>The asynchronous task whose result contains the checkout payload</returns>
        public string GetCheckoutRequest(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            _genericAttributeService.SaveAttribute<string>(postProcessPaymentRequest.Order,
                SezzleHelper.OrderReferenceId,
                postProcessPaymentRequest.Order.OrderGuid.ToString());

            //get store location
            var storeLocation = _webHelper.GetStoreLocation();
            var currentCustomer = EngineContext.Current.Resolve<IWorkContext>().CurrentCustomer;

            var billingAddress = _addressService.GetAddressById(postProcessPaymentRequest.Order.BillingAddressId);
            var shippingAddress = _addressService.GetAddressById(postProcessPaymentRequest.Order.ShippingAddressId ?? 0);

            var orderItems = new List<ItemPayload>();
            foreach (var item in _orderService.GetOrderItems(postProcessPaymentRequest.Order.Id))
            {
                var product = _productService.GetProductById(item.ProductId);
                orderItems.Add(
                    new ItemPayload
                    {
                        Name = product.Name,
                        Sku = product.Sku,
                        Quantity = item.Quantity,
                        Price = new PricePayload
                        {
                            AmountInCents = (long)Math.Round((item.PriceExclTax * 100), 2),
                            Currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode
                        }
                    }
                );
            }

            var request = new ObtainCheckoutRequest
            {
                AmountInCents = (long)Math.Round((postProcessPaymentRequest.Order.OrderTotal * 100), 2),
                OrderDescription = SezzleHelper.OrderDescription,
                CurrencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode,
                OrderReferenceId = postProcessPaymentRequest.Order.OrderGuid.ToString(),
                CheckoutCancelUrl = new Uri($"{storeLocation}Plugins/PaymentSezzle/CancelOrder?reference_id={postProcessPaymentRequest.Order.OrderGuid.ToString()}"),
                CheckoutCompleteUrl = new Uri($"{storeLocation}Plugins/PaymentSezzle/CompleteOrder?reference_id={postProcessPaymentRequest.Order.OrderGuid.ToString()}"),
                CustomerDetails = new CustomerPayload
                {
                    FirstName = billingAddress?.FirstName,
                    LastName = billingAddress?.LastName,
                    Email = currentCustomer.Email,
                    Phone = billingAddress?.PhoneNumber
                },
                BillingAddress = new AddressPayload
                {
                    Name = String.Concat(billingAddress?.FirstName, " ", billingAddress?.LastName),
                    Street = billingAddress?.Address1,
                    Street2 = billingAddress?.Address2,
                    City = billingAddress?.City,
                    State = _stateProvinceService.GetStateProvinceByAddress(billingAddress)?.Abbreviation,
                    CountryCode = _countryService.GetCountryByAddress(billingAddress)?.TwoLetterIsoCode,
                    PostalCode = billingAddress?.ZipPostalCode,
                    PhoneNumber = billingAddress?.PhoneNumber
                },
                ShippingAddress = new AddressPayload
                {
                    Name = String.Concat(shippingAddress?.FirstName, " ", shippingAddress?.LastName),
                    Street = shippingAddress?.Address1,
                    Street2 = shippingAddress?.Address2,
                    City = shippingAddress?.City,
                    State = _stateProvinceService.GetStateProvinceByAddress(shippingAddress)?.Abbreviation,
                    CountryCode = _countryService.GetCountryByAddress(shippingAddress)?.TwoLetterIsoCode,
                    PostalCode = shippingAddress?.ZipPostalCode,
                    PhoneNumber = shippingAddress?.PhoneNumber
                },
                RequiresShippingInfo = false,
                Items = orderItems,
                Discounts = new List<DiscountPayload>
                {
                    new DiscountPayload
                    {
                        Name = postProcessPaymentRequest.Order.OrderDiscount.ToString(),
                        Amount = new PricePayload
                        {
                            AmountInCents = (long)Math.Round((postProcessPaymentRequest.Order.OrderDiscount * 100), 2),
                            Currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode
                        }
                    }
                },
                TaxAmount = new PricePayload
                {
                    AmountInCents = (long)Math.Round((postProcessPaymentRequest.Order.OrderTax * 100), 2),
                    Currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode
                },
                ShippingAmount = new PricePayload
                {
                    AmountInCents = (long)Math.Round((postProcessPaymentRequest.Order.OrderShippingExclTax * 100), 2),
                    Currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode
                },
                MerchantCompletes = true
            };

            var jsonRequest = JsonConvert.SerializeObject(request);
            return jsonRequest;
        }

        /// <summary>
        /// Get Sezzle checkout url
        /// </summary>
        /// <param name="postProcessPaymentRequest">Post process payment request</param>
        /// <returns>The asynchronous task whose result contains the redirect url</returns>
        public async Task<string> GetCheckoutUrl(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            try
            {
                //get response
                var url = _sezzlePaymentSettings.UseSandbox ?
                    "https://sandbox.gateway.sezzle.in/v1/checkouts" :
                    "https://gateway.sezzle.in/v1/checkouts";
                var authToken = GetAuthToken().GetAwaiter().GetResult();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                var request = GetCheckoutRequest(postProcessPaymentRequest);

                var requestContent = new StringContent(request,
                    Encoding.UTF8, MimeTypes.ApplicationJson);
                var response = await _httpClient.PostAsync(url, requestContent);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var checkoutResponse = JsonConvert.DeserializeObject<ObtainCheckoutResponse>(responseContent);
                return checkoutResponse?.CheckoutUrl;
            }
            catch(AggregateException exception)
            {
                throw exception.InnerException;
            }
        }

        #endregion
    }
}