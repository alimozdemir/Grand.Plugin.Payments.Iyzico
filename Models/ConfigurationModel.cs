using Grand.Framework.Localization;
using Grand.Framework.Mvc.ModelBinding;
using Grand.Framework.Mvc.Models;
using System.Collections.Generic;

namespace Grand.Plugin.Payments.Iyzico.Models
{
    public class ConfigurationModel : BaseGrandModel, ILocalizedModel<ConfigurationModel.ConfigurationLocalizedModel>
    {
        public ConfigurationModel()
        {
            Locales = new List<ConfigurationLocalizedModel>();
        }

        public string ActiveStoreScopeConfiguration { get; set; }
        
        [GrandResourceDisplayName("Plugins.Payment.Iyzico.DescriptionText")]
        public string DescriptionText { get; set; }
        public bool DescriptionText_OverrideForStore { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.Iyzico.APIUrl")]
        public string APIUrl { get; set; }
        public bool APIUrl_OverrideForStore { get; set; }
        
        [GrandResourceDisplayName("Plugins.Payment.Iyzico.APIKey")]
        public string APIKey { get; set; }
        public bool APIKey_OverrideForStore { get; set; }
        
        [GrandResourceDisplayName("Plugins.Payment.Iyzico.SecretKey")]
        public string SecretKey { get; set; }
        public bool SecretKey_OverrideForStore { get; set; }
        
        [GrandResourceDisplayName("Plugins.Payment.Iyzico.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.Iyzico.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.Iyzico.ShippableProductRequired")]
        public bool ShippableProductRequired { get; set; }
        public bool ShippableProductRequired_OverrideForStore { get; set; }

        public IList<ConfigurationLocalizedModel> Locales { get; set; }

        #region Nested class

        public partial class ConfigurationLocalizedModel : ILocalizedModelLocal
        {
            public string LanguageId { get; set; }
            
            [GrandResourceDisplayName("Plugins.Payment.Iyzico.DescriptionText")]
            public string DescriptionText { get; set; }
        }

        #endregion
    }
}