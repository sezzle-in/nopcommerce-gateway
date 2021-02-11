﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Sezzle.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //PDT
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Sezzle.CompleteOrder", "Plugins/PaymentSezzle/CompleteOrder",
                 new { controller = "PaymentSezzle", action = "CompleteOrder", });

            //Cancel
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Sezzle.CancelOrder", "Plugins/PaymentSezzle/CancelOrder",
                 new { controller = "PaymentSezzle", action = "CancelOrder" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => -1;
    }
}