using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nop.Plugin.Payments.Sezzle.Payload
{
    /// <summary>
    /// Represents request parameters to obtain auth token
    /// </summary>
    public class ObtainAuthTokenRequest
    {
        /// <summary>
        /// Gets or sets public key
        /// </summary>
        [JsonProperty(PropertyName = "public_key")]
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets private key
        /// </summary>
        [JsonProperty(PropertyName = "private_key")]
        public string PrivateKey { get; set; }

    }
}