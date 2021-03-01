using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sezzle.Payload
{
    public class ObtainOrderResponse
    {
        /// <summary>
        /// Gets or sets created at
        /// </summary>
        [JsonProperty(PropertyName = "created_at")]
        public string CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets captured at
        /// </summary>
        [JsonProperty(PropertyName = "captured_at")]
        public string CapturedAt { get; set; }

        /// <summary>
        /// Gets or sets capture expiration
        /// </summary>
        [JsonProperty(PropertyName = "capture_expiration")]
        public string CaptureExpiration { get; set; }

        /// <summary>
        /// Gets or sets description
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets amount in cents
        /// </summary>
        [JsonProperty(PropertyName = "amount_in_cents")]
        public long AmountInCents { get; set; }

        /// <summary>
        /// Gets or sets usd amount in cents
        /// </summary>
        [JsonProperty(PropertyName = "usd_amount_in_cents")]
        public long UsdAmountInCents { get; set; }

        /// <summary>
        /// Gets or sets customer amount in cents
        /// </summary>
        [JsonProperty(PropertyName = "customer_amount_in_cents")]
        public long CustomerAmountInCents { get; set; }

        /// <summary>
        /// Gets or sets currency code
        /// </summary>
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets customer currency code
        /// </summary>
        [JsonProperty("customer_currency_code")]
        public string CustomerCurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets reference id
        /// </summary>
        [JsonProperty("reference_id")]
        public string ReferenceId { get; set; }

        /// <summary>
        /// Gets or sets customer
        /// </summary>
        [JsonProperty("customer")]
        public CustomerPayload Customer { get; set; }

        /// <summary>
        /// Gets or sets shipping address
        /// </summary>
        [JsonProperty("shipping_address")]
        public AddressPayload ShippingAddress { get; set; }

        /// <summary>
        /// Gets or sets billing address
        /// </summary>
        [JsonProperty("billing_address")]
        public AddressPayload BillingAddress { get; set; }

        /// <summary>
        /// Gets or sets refunds
        /// </summary>
        [JsonProperty("refunds")]
        public List<RefundPayload> Refunds { get; set; }
    }
}
