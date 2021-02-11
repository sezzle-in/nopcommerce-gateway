using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sezzle.Payload
{
    public class RefundPayload
    {

        /// <summary>
        /// Gets or sets amount
        /// </summary>
        [JsonProperty("amount")]
        public PricePayload Amount { get; set; }

        /// <summary>
        /// Gets or sets is full refund
        /// </summary>
        [JsonProperty("is_full_refund")]
        public bool IsFullRefund { get; set; }

        /// <summary>
        /// Gets or sets order reference id
        /// </summary>
        [JsonProperty("order_reference_id")]
        public string OrderReferenceId { get; set; }

        /// <summary>
        /// Gets or sets refund id
        /// </summary>
        [JsonProperty("refund_id")]
        public string RefundId { get; set; }

        /// <summary>
        /// Gets or sets refund reason
        /// </summary>
        [JsonProperty("refund_reason")]
        public string RefundReason { get; set; }
    }
}
