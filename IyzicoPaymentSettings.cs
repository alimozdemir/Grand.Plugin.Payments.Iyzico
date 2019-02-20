using Grand.Core.Configuration;

namespace Grand.Plugin.Payments.Iyzico
{
    public class IyzicoPaymentSettings : ISettings
    {
        public string APIUrl { get; set; }
        public string APIKey { get; set; }
        public string SecretKey { get; set; }
        public string DescriptionText { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether shippable products are required in order to display this payment method during checkout
        /// </summary>
        public bool ShippableProductRequired { get; set; }
    }
}
