using System;
using System.Linq;
using Grand.Core;
using Grand.Plugin.Payments.Iyzico.Models;
using Grand.Services.Configuration;
using Grand.Services.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Plugin.Payments.Iyzico.Components
{
    public class PaymentIyzicoViewComponent : ViewComponent
    {
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        public PaymentIyzicoViewComponent(IWorkContext workContext,   
            ISettingService settingService,
            IStoreContext storeContext)
        {
            this._workContext = workContext;
            this._settingService = settingService;
            this._storeContext = storeContext;

        }

        public IViewComponentResult Invoke()
        {
            var iyzicoPaymentSettings = _settingService.LoadSetting<IyzicoPaymentSettings>(_storeContext.CurrentStore.Id);
            
            
            var model = new PaymentInfoModel();
            
            //CC types
            model.CreditCardTypes.Add(new SelectListItem
                {
                    Text = "Visa",
                    Value = "Visa",
                });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "Master card",
                Value = "MasterCard",
            });
            
            //years
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem
                {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i : i.ToString();
                model.ExpireMonths.Add(new SelectListItem
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            //set postback values
            // only for onepagecheckout !
            if (this.Request.HasFormContentType)
            {
                var form = this.Request.Form;
                if (form != null)
                {
                    model.CardholderName = form["CardholderName"];
                    model.CardNumber = form["CardNumber"];
                    model.CardCode = form["CardCode"];
                    var selectedCcType = model.CreditCardTypes.FirstOrDefault(x => x.Value.Equals(form["CreditCardType"], StringComparison.InvariantCultureIgnoreCase));
                    if (selectedCcType != null)
                        selectedCcType.Selected = true;
                    var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
                    if (selectedMonth != null)
                        selectedMonth.Selected = true;
                    var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
                    if (selectedYear != null)
                        selectedYear.Selected = true;   
                }                
            }


            return View("~/Plugins/Payments.Iyzico/Views/PaymentIyzico/PaymentInfo.cshtml", model);
        }
    }
}