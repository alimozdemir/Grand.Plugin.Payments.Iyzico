using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentValidation.Attributes;
using Grand.Framework.Mvc.ModelBinding;
using Grand.Framework.Mvc.Models;
using Grand.Plugin.Payments.Iyzico.Validators;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Plugin.Payments.Iyzico.Models
{
    
    [Validator(typeof(PaymentInfoValidator))]
    public class PaymentInfoModel : BaseGrandModel
    {
        // public string DescriptionText { get; set; }
        
        public PaymentInfoModel()
        {
            CreditCardTypes = new List<SelectListItem>();
            ExpireMonths = new List<SelectListItem>();
            ExpireYears = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Payment.SelectCreditCard")]
        public string CreditCardType { get; set; }
        [GrandResourceDisplayName("Payment.SelectCreditCard")]
        public IList<SelectListItem> CreditCardTypes { get; set; }

        [GrandResourceDisplayName("Payment.CardholderName")]
        public string CardholderName { get; set; }

        [GrandResourceDisplayName("Payment.CardNumber")]

        public string CardNumber { get; set; }

        [GrandResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireMonth { get; set; }
        [GrandResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireYear { get; set; }
        
        public IList<SelectListItem> ExpireMonths { get; set; }
        public IList<SelectListItem> ExpireYears { get; set; }

        [GrandResourceDisplayName("Payment.CardCode")]
        public string CardCode { get; set; }
        
        
        [GrandResourceDisplayName("Payment.Secure3d")]
        public bool Secure3D { get; set; }
        
        [GrandResourceDisplayName("Payment.CardCode")]
        public string Test3DValue { get; set; }
    }
}