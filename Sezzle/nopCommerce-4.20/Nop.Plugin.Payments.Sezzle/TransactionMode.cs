
namespace Nop.Plugin.Payments.Sezzle
{
    /// <summary>
    /// Represents manual payment processor transaction mode
    /// </summary>
    public enum TransactionMode
    {

        /// <summary>
        /// Authorize
        /// </summary>
        Authorize = 1,

        /// <summary>
        /// Authorize and capture
        /// </summary>
        AuthorizeAndCapture= 2
    }
}
