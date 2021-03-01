using Nop.Core.Domain.Payments;

namespace Nop.Plugin.Payments.Sezzle
{
    /// <summary>
    /// Represents Sezzle helper
    /// </summary>
    public class SezzleHelper
    {
        #region Properties

        /// <summary>
        /// Get nopCommerce partner code
        /// </summary>
        public static string OrderDescription => "NopCommerce-4.20";

        public static string OrderReferenceId => "OrderReferenceId";

        #endregion
    }
}