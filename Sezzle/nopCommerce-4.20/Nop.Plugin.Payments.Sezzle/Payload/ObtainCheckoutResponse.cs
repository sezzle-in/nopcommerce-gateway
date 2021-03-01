using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sezzle.Payload
{
    public class ObtainCheckoutResponse
    {

        /// <summary>
        /// Gets or sets checkout url
        /// </summary>
        [JsonProperty( PropertyName = "checkout_url")]
        public string CheckoutUrl { get; set; }

        /// <summary>
        /// Gets or sets checkout uuid
        /// </summary>
        [JsonProperty(PropertyName = "checkout_uuid")]
        public string CheckoutUuid { get; set; }
    }
}
