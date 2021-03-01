using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Sezzle.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="routeBuilder">Route builder</param>
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //PDT
            routeBuilder.MapRoute("Plugin.Payments.Sezzle.CompleteOrder", "Plugins/PaymentSezzle/CompleteOrder",
                 new { controller = "PaymentSezzle", action = "CompleteOrder", });

            //Cancel
            routeBuilder.MapRoute("Plugin.Payments.Sezzle.CancelOrder", "Plugins/PaymentSezzle/CancelOrder",
                 new { controller = "PaymentSezzle", action = "CancelOrder" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => -1;
    }
}