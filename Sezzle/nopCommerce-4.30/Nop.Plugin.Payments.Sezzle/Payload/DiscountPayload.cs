using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sezzle.Payload
{
    public class DiscountPayload
    {

        /// <summary>
        /// Gets or sets name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets amount
        /// </summary>
        [JsonProperty("amount")]
        public PricePayload Amount { get; set; }
    }
}
