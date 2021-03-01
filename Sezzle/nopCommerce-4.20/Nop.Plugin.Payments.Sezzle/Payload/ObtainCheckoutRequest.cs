using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sezzle.Payload
{
    public class ObtainCheckoutRequest
    {
        /// <summary>
        /// Gets or sets amount in cents
        /// </summary>
        [JsonProperty(PropertyName = "amount_in_cents")]
        public long AmountInCents { get; set; }

        /// <summary>
        /// Gets or sets order description
        /// </summary>
        [JsonProperty(PropertyName = "order_description")]
        public string OrderDescription { get; set; }

        /// <summary>
        /// Gets or sets currency code
        /// </summary>
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets order reference id
        /// </summary>
        [JsonProperty("order_reference_id")]
        public string OrderReferenceId { get; set; }

        /// <summary>
        /// Gets or sets checkout cancel url
        /// </summary>
        [JsonProperty("checkout_cancel_url")]
        public Uri CheckoutCancelUrl { get; set; }

        /// <summary>
        /// Gets or sets checkout complete url
        /// </summary>
        [JsonProperty("checkout_complete_url")]
        public Uri CheckoutCompleteUrl { get; set; }

        /// <summary>
        /// Gets or sets customer details
        /// </summary>
        [JsonProperty("customer_details")]
        public CustomerPayload CustomerDetails { get; set; }

        /// <summary>
        /// Gets or sets billing address
        /// </summary>
        [JsonProperty("billing_address")]
        public AddressPayload BillingAddress { get; set; }

        /// <summary>
        /// Gets or sets shipping address
        /// </summary>
        [JsonProperty("shipping_address")]
        public AddressPayload ShippingAddress { get; set; }

        /// <summary>
        /// Gets or sets requires shipping info
        /// </summary>
        [JsonProperty("requires_shipping_info")]
        public bool RequiresShippingInfo { get; set; }

        /// <summary>
        /// Gets or sets items
        /// </summary>
        [JsonProperty("items")]
        public List<ItemPayload> Items { get; set; }

        /// <summary>
        /// Gets or sets discounts
        /// </summary>
        [JsonProperty("discounts")]
        public List<DiscountPayload> Discounts { get; set; }

        /// <summary>
        /// Gets or sets tax amount
        /// </summary>
        [JsonProperty("tax_amount")]
        public PricePayload TaxAmount { get; set; }

        /// <summary>
        /// Gets or sets shipping amount
        /// </summary>
        [JsonProperty("shipping_amount")]
        public PricePayload ShippingAmount { get; set; }

        /// <summary>
        /// Gets or sets merchant completes
        /// </summary>
        [JsonProperty("merchant_completes")]
        public bool MerchantCompletes { get; set; }
    }
}
