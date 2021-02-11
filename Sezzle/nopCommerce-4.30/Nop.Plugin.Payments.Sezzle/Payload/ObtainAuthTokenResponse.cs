using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sezzle.Payload
{
    /// <summary>
    /// Represents returned values of request to obtain auth token 
    /// </summary>
    public class ObtainAuthTokenResponse
    {
        /// <summary>
        /// Gets or sets auth token
        /// </summary>
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets expiration date
        /// </summary>
        [JsonProperty(PropertyName = "expiration_date")]
        public string ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets merchant uuid
        /// </summary>
        [JsonProperty(PropertyName = "merchant_uuid")]
        public string MerchantUuid { get; set; }
    }
}