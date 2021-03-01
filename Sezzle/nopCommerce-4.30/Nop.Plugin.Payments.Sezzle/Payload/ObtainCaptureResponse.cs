using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sezzle.Payload
{
    public class ObtainCaptureResponse
    {
        /// <summary>
        /// Gets or sets status
        /// </summary>
        [JsonProperty("status")]
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
