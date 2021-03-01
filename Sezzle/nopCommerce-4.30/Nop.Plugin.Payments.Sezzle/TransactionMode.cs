
namespace Nop.Plugin.Payments.Sezzle
{
    /// <summary>
    /// Represents manual payment processor transaction mode
    /// </summary>
    public enum TransactionMode
    {

        /// <summary>
        /// Authorize Only
        /// </summary>
        AuthorizeOnly = 1,

        /// <summary>
        /// Authorize and Capture
        /// </summary>
        AuthorizeAndCapture= 2
    }
}
