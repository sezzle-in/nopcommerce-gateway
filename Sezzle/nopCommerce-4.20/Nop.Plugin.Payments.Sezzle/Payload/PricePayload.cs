using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sezzle.Payload
{
    public class PricePayload
    {

        /// <summary>
        /// Gets or sets amount in cents
        /// </summary>
        [JsonProperty("amount_in_cents")]
        public long AmountInCents { get; set; }

        /// <summary>
        /// Gets or sets currency
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}
