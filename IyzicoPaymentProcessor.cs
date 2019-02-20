using Grand.Core;
using Grand.Core.Domain.Orders;
using Grand.Core.Domain.Payments;
using Grand.Core.Plugins;
using Grand.Plugin.Payments.Iyzico.Controllers;
using Grand.Services.Configuration;
using Grand.Services.Localization;
using Grand.Services.Orders;
using Grand.Services.Payments;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Armut.Iyzipay.Model;
using Armut.Iyzipay.Request;
using Grand.Core.Domain.Customers;
using Grand.Plugin.Payments.Iyzico.Models;
using Grand.Plugin.Payments.Iyzico.Validators;
using Grand.Services.Catalog;
using Grand.Services.Common;
using Grand.Services.Customers;
using Grand.Services.Directory;
using Grand.Services.Security;

namespace Grand.Plugin.Payments.Iyzico
{
    /// <summary>
    /// Iyzico payment processor
    /// </summary>
    public class IyzicoPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly IyzicoPaymentSettings _iyzicoPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly IStoreContext _storeContext;
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerSerivce;
        private readonly ICountryService _countryService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEncryptionService _encryptionService;

        #endregion

        #region Ctor

        public IyzicoPaymentProcessor(IyzicoPaymentSettings iyzicoPaymentSettings,
            ISettingService settingService, IOrderTotalCalculationService orderTotalCalculationService,
            ILocalizationService localizationService, IWebHelper webHelper, IStoreContext storeContext,
            IOrderService orderService,
            ICustomerService customerService,
            ICountryService countryService,
            IProductService productService,
            ICategoryService categoryService,
            IEncryptionService encryptionService,
            IHttpContextAccessor httpContextAccessor)
        {
            this._iyzicoPaymentSettings = iyzicoPaymentSettings;
            this._settingService = settingService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
            this._storeContext = storeContext;
            this._orderService = orderService;
            this._customerSerivce = customerService;
            this._countryService = countryService;
            this._productService = productService;
            this._categoryService = categoryService;
            this._httpContextAccessor = httpContextAccessor;
            this._encryptionService = encryptionService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentIyzico/Configure";
        }


        private Armut.Iyzipay.Options IyzicoConfig()
        {
            var iyzicoPaymentSettings =
                _settingService.LoadSetting<IyzicoPaymentSettings>(_storeContext.CurrentStore.Id);

            var options = new Armut.Iyzipay.Options();

            options.ApiKey = iyzicoPaymentSettings.APIKey;
            options.SecretKey = iyzicoPaymentSettings.SecretKey;
            options.BaseUrl = iyzicoPaymentSettings.APIUrl;
            return options;
        }
        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            
            // should go to 3d secure payment
            if (!(processPaymentRequest.CustomValues.TryGetValue("Secure3d", out var value) &&
                value.ToString().Equals("Yes")))
            {
                // main problem here, this method does not getting real order information from the system.
                // therefore first, we have to sent customer billing/shipping information to the iyzico.
                // Then we can send the real information
                // I won't gonna alter this method for the open-source purposes. But, I will alter it on my project.
                var customer = _customerSerivce.GetCustomerById(processPaymentRequest.CustomerId);
                var options = IyzicoConfig();

                CreatePaymentRequest request = new CreatePaymentRequest();
                request.ConversationId = processPaymentRequest.OrderGuid.ToString();
                request.Price = processPaymentRequest.OrderTotal.ToString();
                request.PaidPrice = processPaymentRequest.OrderTotal.ToString();

                request.Currency = Currency.TRY.ToString();
                request.Installment = 1;
                request.BasketId = processPaymentRequest.OrderGuid.ToString();
                request.PaymentChannel = PaymentChannel.WEB.ToString();
                request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

                PaymentCard paymentCard = new PaymentCard();
                paymentCard.CardHolderName = processPaymentRequest.CreditCardName;
                paymentCard.CardNumber = processPaymentRequest.CreditCardNumber;
                paymentCard.ExpireMonth = processPaymentRequest.CreditCardExpireMonth.ToString();
                paymentCard.ExpireYear = processPaymentRequest.CreditCardExpireYear.ToString();
                paymentCard.Cvc = processPaymentRequest.CreditCardCvv2;
                paymentCard.RegisterCard = 0;
                
                request.PaymentCard = paymentCard;

                Buyer buyer = new Buyer();
                buyer.Id = customer.Id;
                buyer.Name = customer.GetAttribute<string>(SystemCustomerAttributeNames.FirstName);
                buyer.Surname = customer.GetAttribute<string>(SystemCustomerAttributeNames.LastName);
                // SystemCustomerAttributeNames.ImpersonatedCustomerId
                // buyer.GsmNumber = "+905350000000";
                buyer.Email = customer.Email;
                buyer.IdentityNumber = "74300864791";
                // buyer.LastLoginDate = "2015-10-05 12:43:35";
                // buyer.RegistrationDate = "2013-04-21 15:12:09";
                var address = customer.BillingAddress != null
                    ? customer.BillingAddress.Address1
                    : "No address registered";
                if (customer.BillingAddress != null)
                {
                    buyer.RegistrationAddress = customer.BillingAddress.Address1;
                    buyer.City = customer.BillingAddress.City;
                    var country = _countryService.GetCountryById(customer.BillingAddress.CountryId);
                    buyer.Country = country != null ? country.Name : "USA";
                    buyer.ZipCode = customer.BillingAddress.ZipPostalCode;
                }
                else
                {
                    buyer.RegistrationAddress = "No address registered";
                    buyer.City = "New York";
                    buyer.Country = "USA";
                    buyer.ZipCode = "00000";
                }

                buyer.RegistrationAddress = address;
                buyer.Ip = customer.LastIpAddress;
                // buyer.City = customer.Addresses.;
                // buyer.Country = "Turkey";
                // buyer.ZipCode = "34732";
                request.Buyer = buyer;
                // This addresses are not right for this part.
                // We have to get the order's addresses. This method need an improvement.
                Address shippingAddress = new Address();
                var shippingCountry = _countryService.GetCountryById(customer.ShippingAddress.CountryId);
                shippingAddress.ContactName =
                    customer.ShippingAddress.FirstName + " " + customer.ShippingAddress.LastName;
                shippingAddress.City = customer.ShippingAddress.City;
                shippingAddress.Country = shippingCountry.Name;
                shippingAddress.Description =
                    customer.ShippingAddress.Address1 + " " + customer.ShippingAddress.Address2;
                shippingAddress.ZipCode = customer.ShippingAddress.ZipPostalCode;
                request.ShippingAddress = shippingAddress;

                var customerBillingAddress =
                    customer.BillingAddress ?? customer.ShippingAddress;

                Address billingAddress = new Address();
                var billingCountry = _countryService.GetCountryById(customerBillingAddress.CountryId);
                billingAddress.ContactName = customerBillingAddress.FirstName + " " + customerBillingAddress.LastName;
                billingAddress.City = customerBillingAddress.City;
                billingAddress.Country = billingCountry.Name;
                billingAddress.Description = customerBillingAddress.Address1 + " " + customerBillingAddress.Address2;
                billingAddress.ZipCode = customerBillingAddress.ZipPostalCode;
                request.BillingAddress = billingAddress;

                List<BasketItem> basketItems = new List<BasketItem>();
                var products = customer.ShoppingCartItems.Select(i => _productService.GetProductById(i.ProductId));
                // var product = _productService.GetProductById();
                BasketItem basketItem = new BasketItem();
                basketItem.Id = string.Join(',', products.Select(i => i.Id));
                basketItem.Name = string.Join(',', products.Select(i => i.Name));
                basketItem.Category1 = "Product";

                basketItem.ItemType = BasketItemType.PHYSICAL.ToString();
                basketItem.Price = processPaymentRequest.OrderTotal.ToString();
                basketItems.Add(basketItem);

                request.BasketItems = basketItems;

                Payment payment = Payment.Create(request, options);

                if (payment.Status.Equals("success"))
                {
                    result.NewPaymentStatus = PaymentStatus.Paid;
                }
                else if (payment.Status.Equals("failure"))
                {
                    // if error code is different than secure 3d error code
                    // DEBIT_CARDS_REQUIRES_3DS
                    if (!payment.ErrorCode.Equals("10217"))
                    {
                        result.NewPaymentStatus = PaymentStatus.Voided;

                        result.AddError(payment.ErrorMessage);
                        result.CaptureTransactionId = payment.PaymentId;
                    }
                }
            }

            // this is only for process the card information to PostProcessPayment method.
            // after that we will clear the card information on PaymentIyzicoController
            if (result.NewPaymentStatus == PaymentStatus.Pending)
                result.AllowStoringCreditCardNumber = true;

            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var customer = _customerSerivce.GetCustomerById(postProcessPaymentRequest.Order.CustomerId);

            var options = IyzicoConfig();

            var request = new CreatePaymentRequest();
            request.ConversationId = postProcessPaymentRequest.Order.OrderGuid.ToString();
            request.Price = postProcessPaymentRequest.Order.OrderTotal.ToString();
            request.PaidPrice = postProcessPaymentRequest.Order.OrderTotal.ToString();

            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = postProcessPaymentRequest.Order.OrderGuid.ToString();
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            PaymentCard paymentCard = new PaymentCard();
            paymentCard.CardHolderName = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardName);
            paymentCard.CardNumber = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardNumber);
            paymentCard.ExpireMonth =
                _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardExpirationMonth.ToString());
            paymentCard.ExpireYear =
                _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardExpirationYear.ToString());
            paymentCard.Cvc = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardCvv2);
            paymentCard.RegisterCard = 0;
            // paymentCard.
            request.PaymentCard = paymentCard;

            Buyer buyer = new Buyer();
            buyer.Id = postProcessPaymentRequest.Order.CustomerId;
            buyer.Name = customer.GetAttribute<string>(SystemCustomerAttributeNames.FirstName);
            buyer.Surname = customer.GetAttribute<string>(SystemCustomerAttributeNames.LastName);
            // SystemCustomerAttributeNames.ImpersonatedCustomerId
            // buyer.GsmNumber = "+905350000000";
            buyer.Email = postProcessPaymentRequest.Order.CustomerEmail;
            buyer.IdentityNumber = "74300864791";
            // buyer.LastLoginDate = "2015-10-05 12:43:35";
            // buyer.RegistrationDate = "2013-04-21 15:12:09";
            var address = postProcessPaymentRequest.Order.BillingAddress != null ? 
                postProcessPaymentRequest.Order.BillingAddress.Address1 : "No address registred";
            buyer.RegistrationAddress = postProcessPaymentRequest.Order.BillingAddress.Address1;
            buyer.City = postProcessPaymentRequest.Order.BillingAddress.City;
            var country = _countryService.GetCountryById(postProcessPaymentRequest.Order.BillingAddress.CountryId);
            buyer.Country = country != null ? country.Name : "USA";
            buyer.ZipCode = postProcessPaymentRequest.Order.BillingAddress.ZipPostalCode;
            buyer.RegistrationAddress = address;
            buyer.Ip = customer.LastIpAddress;
            // buyer.City = customer.Addresses.;
            // buyer.Country = "Turkey";
            // buyer.ZipCode = "34732";
            request.Buyer = buyer;
            // This addresses are not right for this part. We have to get the order's addresses. This method need an improvement.
            Address shippingAddress = new Address();
            var shippingCountry = _countryService.GetCountryById(postProcessPaymentRequest.Order.ShippingAddress.CountryId);
            shippingAddress.ContactName = postProcessPaymentRequest.Order.ShippingAddress.FirstName + " " + postProcessPaymentRequest.Order.ShippingAddress.LastName;
            shippingAddress.City = postProcessPaymentRequest.Order.ShippingAddress.City;
            shippingAddress.Country = shippingCountry.Name;
            shippingAddress.Description = postProcessPaymentRequest.Order.ShippingAddress.Address1 + " " + postProcessPaymentRequest.Order.ShippingAddress.Address2;
            shippingAddress.ZipCode = postProcessPaymentRequest.Order.ShippingAddress.ZipPostalCode;
            request.ShippingAddress = shippingAddress;


            Address billingAddress = new Address();
            var billingCountry = _countryService.GetCountryById(postProcessPaymentRequest.Order.BillingAddress.CountryId);
            billingAddress.ContactName = postProcessPaymentRequest.Order.BillingAddress.FirstName + " " + postProcessPaymentRequest.Order.BillingAddress.LastName;
            billingAddress.City = postProcessPaymentRequest.Order.BillingAddress.City;
            billingAddress.Country = billingCountry.Name;
            billingAddress.Description = postProcessPaymentRequest.Order.BillingAddress.Address1 + " " + postProcessPaymentRequest.Order.BillingAddress.Address2;
            billingAddress.ZipCode = postProcessPaymentRequest.Order.BillingAddress.ZipPostalCode;
            request.BillingAddress = billingAddress;

            List<BasketItem> basketItems = new List<BasketItem>();
            var products = postProcessPaymentRequest.Order.OrderItems.Select(i => _productService.GetProductById(i.ProductId));
            // var product = _productService.GetProductById();
            BasketItem basketItem = new BasketItem();
            basketItem.Id = string.Join(',', products.Select(i => i.Id));
            basketItem.Name = string.Join(',', products.Select(i => i.Name));
            basketItem.Category1 = "Etiket";

            basketItem.ItemType = BasketItemType.PHYSICAL.ToString();
            basketItem.Price = postProcessPaymentRequest.Order.OrderTotal.ToString();
            basketItems.Add(basketItem);

            request.BasketItems = basketItems;
            var loc = _webHelper.GetStoreLocation();

            request.CallbackUrl = $"{loc}Plugins/Iyzico/Handler";

            var payment = ThreedsInitialize.Create(request, options);

            if (payment.Status.Equals("success"))
            {
                _httpContextAccessor.HttpContext.Session.SetString("Iyzico.HtmlContent", payment.HtmlContent);
                var htmlContentRedirect = $"{loc}Plugins/Iyzico/ThreeDPayment";
                _httpContextAccessor.HttpContext.Response.Redirect(htmlContentRedirect);
            }
            else
            {
                throw new Exception(payment.ErrorMessage); 
            }
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country

            /* if (_iyzicoPaymentSettings.ShippableProductRequired && !cart.RequiresShipping())
                return true;*/

            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _iyzicoPaymentSettings.AdditionalFee, _iyzicoPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //it's not a redirection payment method. So we always return false
            return false;
        }

        public Type GetControllerType()
        {
            return typeof(PaymentIyzicoController);
        }

        public override void Install()
        {
            var settings = new IyzicoPaymentSettings
            {
                DescriptionText =
                    "<p>In cases where an order is placed, an authorized representative will contact you, personally or over telephone, to confirm the order.<br />After the order is confirmed, it will be processed.<br />Orders once confirmed, cannot be cancelled.</p><p>P.S. You can edit this text from admin panel.</p>"
            };
            _settingService.SaveSetting(settings);

            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.DescriptionText", "Description");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.DescriptionText.Hint",
                "Enter info that will be shown to customers during checkout");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.PaymentMethodDescription", "Cash On Delivery");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.AdditionalFee.Hint", "The additional fee.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.AdditionalFeePercentage",
                "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.AdditionalFeePercentage.Hint",
                "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.ShippableProductRequired",
                "Shippable product required");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.ShippableProductRequired.Hint",
                "An option indicating whether shippable products are required in order to display this payment method during checkout.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.APIUrl", "API Url (sandbox or real one).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.APIKey", "API Key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.Iyzico.SecretKey", "Secret Key.");
            this.AddOrUpdatePluginLocaleResource("Payment.Secure3d", "Secure 3D");


            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<IyzicoPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.DescriptionText");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.DescriptionText.Hint");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.PaymentMethodDescription");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.ShippableProductRequired");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.ShippableProductRequired.Hint");

            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.APIUrl");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.APIKey");
            this.DeletePluginLocaleResource("Plugins.Payment.Iyzico.SecretKey");
            this.DeletePluginLocaleResource("Payment.Secure3d");


            base.Uninstall();
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                    warnings.Add(error.ErrorMessage);
            return warnings;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CreditCardType = form["CreditCardType"];
            paymentInfo.CreditCardName = form["CardholderName"];
            paymentInfo.CreditCardNumber = form["CardNumber"];
            paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            paymentInfo.CreditCardCvv2 = form["CardCode"];
            if (form.ContainsKey("Secure3d"))
            {
                var secure3d = form["Secure3d"];
                paymentInfo.CustomValues.Add("Secure3d", secure3d.Equals("on") ? "Yes" : "No");
            }

            return paymentInfo;
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentIyzico";
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        public string PaymentMethodDescription
        {
            get { return _localizationService.GetResource("Plugins.Payment.Iyzico.PaymentMethodDescription"); }
        }

        #endregion
    }
}