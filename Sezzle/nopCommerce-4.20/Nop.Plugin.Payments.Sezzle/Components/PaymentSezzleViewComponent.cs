using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Sezzle.Components
{
    [ViewComponent(Name = "PaymentSezzle")]
    public class PaymentSezzleViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.Sezzle/Views/PaymentInfo.cshtml");
        }
    }
}
