using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Sezzle
{
    /// <summary>
    /// Represents settings of the Sezzlement plugin
    /// </summary>
    public class SezzlePaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets a merchant id
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// Gets or sets a public key
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets a private key
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Gets or sets the transaction mode
        /// </summary>
        public TransactionMode TransactionMode { get; set; }

        /// <summary>
        /// Gets or sets a min checkout amount
        /// </summary>
        public long MinCheckoutAmount { get; set; }
    }
}
