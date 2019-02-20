using Grand.Framework.Mvc.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Grand.Plugin.Payments.Iyzico
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //Show html
            routeBuilder.MapRoute("Plugin.Payments.Iyzico.ThreeDPayment",
                "Plugins/Iyzico/ThreeDPayment",
                new { controller = "PaymentIyzico", action = "ThreeDPayment" }
            );
            
            //Handle callback
            routeBuilder.MapRoute("Plugin.Payments.Iyzico.Handler",
                "Plugins/Iyzico/Handler",
                new { controller = "PaymentIyzico", action = "Handler" }
            );

        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}