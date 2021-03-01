using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Plugin.Payments.Sezzle.Models
{
    public class ConfigurationModel : BaseNopModel
    {

        /// <summary>
        /// Gets or sets active store scope configuration
        /// </summary>
        public int ActiveStoreScopeConfiguration { get; set; }

        /// <summary>
        /// Gets or sets use sandbox
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Sezzle.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets merchant id
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Sezzle.Fields.MerchantId")]
        public string MerchantId { get; set; }
        public bool MerchantId_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets public key
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Sezzle.Fields.PublicKey")]
        public string PublicKey { get; set; }
        public bool PublicKey_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets private key
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Sezzle.Fields.PrivateKey")]
        public string PrivateKey { get; set; }
        public bool PrivateKey_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets transaction mode
        /// </summary>
        public int TransactionModeId { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Sezzle.Fields.TransactionMode")]
        public SelectList TransactionModeValues { get; set; }
        public bool TransactionModeId_OverrideForStore { get; set; }

        /// <summary>
        /// Gets or sets min checkout amount
        /// </summary>
        [NopResourceDisplayName("Plugins.Payments.Sezzle.Fields.MinCheckoutAmount")]
        public long MinCheckoutAmount { get; set; }
        public bool MinCheckoutAmount_OverrideForStore { get; set; }
    }
}