using Grand.Core;
using Grand.Framework.Controllers;
using Grand.Framework.Mvc.Filters;
using Grand.Plugin.Payments.Iyzico.Models;
using Grand.Services.Configuration;
using Grand.Services.Localization;
using Grand.Services.Stores;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Armut.Iyzipay.Model;
using Armut.Iyzipay.Request;
using Grand.Core.Domain.Orders;
using Grand.Core.Domain.Payments;
using Grand.Plugin.Payments.PayPalStandard.Models;
using Grand.Services.Orders;
using Microsoft.AspNetCore.Http;
using Grand.Services.Logging;

namespace Grand.Plugin.Payments.Iyzico.Controllers
{
    public class PaymentIyzicoController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStoreContext _storeContext;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;


        public PaymentIyzicoController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService,
            ILocalizationService localizationService,
            ILanguageService languageService,
            IStoreContext storeContext,
            IOrderService orderService,
            ILogger logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._languageService = languageService;
            this._httpContextAccessor = httpContextAccessor;
            this._storeContext = storeContext;
            this._orderService = orderService;
            this._logger = logger;
        }

        public IActionResult ThreeDPayment()
        {
            if (_httpContextAccessor.HttpContext.Session.Keys.Contains("Iyzico.HtmlContent"))
            {
                var html = _httpContextAccessor.HttpContext.Session.GetString("Iyzico.HtmlContent");

                return Content(html, "text/html");
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }
        
        [HttpPost]
        public IActionResult Handler([FromForm]Secure3DHandler model)
        {
            if (ModelState.IsValid)
            {
                var order = _orderService.GetOrderByGuid(new Guid(model.conversationId));
                if (order != null)
                {
                    var result = ProcessThePayment(order, model);

                    if (result)
                        return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                }
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }
        
        [NonAction]
        private bool ProcessThePayment(Order order, Secure3DHandler model)
        {
            var result = false;
            var iyzicoPaymentSettings = _settingService.LoadSetting<IyzicoPaymentSettings>(_storeContext.CurrentStore.Id);
            
            Armut.Iyzipay.Options options = new Armut.Iyzipay.Options();

            options.ApiKey = iyzicoPaymentSettings.APIKey;
            options.SecretKey = iyzicoPaymentSettings.SecretKey;
            options.BaseUrl = iyzicoPaymentSettings.APIUrl;
            
            if (model.status.Equals("success"))
            {
                CreateThreedsPaymentRequest request = new CreateThreedsPaymentRequest();
                // request.Locale = Locale.TR.ToString();
                request.ConversationId = model.conversationId;
                request.PaymentId = model.paymentId;
                request.ConversationData = model.conversationData;
                // complete the 3DS
                ThreedsPayment threedsPayment = ThreedsPayment.Create(request, options);

                if (threedsPayment.Status.Equals("success"))
                {
                    result = true;
                    order.OrderStatus = OrderStatus.Processing;
                    order.PaymentStatus = PaymentStatus.Paid;
                }
                else
                {
                    order.OrderStatus = OrderStatus.Cancelled;
                    order.PaymentStatus = PaymentStatus.Voided;
                    OrderError(threedsPayment.ErrorMessage, order);
                }
            }
            else
            {
                order.OrderStatus = OrderStatus.Cancelled;
                order.PaymentStatus = PaymentStatus.Voided;
                OrderError($"Error mdStatus:{model.mdStatus}", order);
            }
            
            // clear card informations
            // if you want to keep that informations ignore the below part
            order.CardCvv2 = string.Empty;
            order.CardName = string.Empty;
            order.CardNumber = string.Empty;
            order.CardExpirationYear = string.Empty;
            order.CardExpirationMonth = string.Empty;
            order.CardType = string.Empty;
            order.MaskedCreditCardNumber = string.Empty;
            order.AllowStoringCreditCardNumber = false;
            
            _orderService.UpdateOrder(order);

            return result;
        }

        [NonAction]
        private void OrderError(string message, Order order)
        {
            //log
            _logger.Error($"Order Payment Error:{order.Id} {message}");
            //order note
            _orderService.InsertOrderNote(new OrderNote
            {
                Note = message,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });
        }
        
        [AuthorizeAdmin]
        [Area("Admin")]
        public IActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var iyzicoPaymentSettings = _settingService.LoadSetting<IyzicoPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.DescriptionText = iyzicoPaymentSettings.DescriptionText;
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.DescriptionText = iyzicoPaymentSettings.GetLocalizedSetting(x => x.DescriptionText, languageId, "", false, false);
            });
            model.APIUrl = iyzicoPaymentSettings.APIUrl;
            model.APIKey = iyzicoPaymentSettings.APIKey;
            model.SecretKey = iyzicoPaymentSettings.SecretKey;
            model.AdditionalFee = iyzicoPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = iyzicoPaymentSettings.AdditionalFeePercentage;
            model.ShippableProductRequired = iyzicoPaymentSettings.ShippableProductRequired;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (!String.IsNullOrEmpty(storeScope))
            {
                model.DescriptionText_OverrideForStore = _settingService.SettingExists(iyzicoPaymentSettings, x => x.DescriptionText, storeScope);
                model.APIUrl_OverrideForStore = _settingService.SettingExists(iyzicoPaymentSettings, x => x.APIUrl, storeScope);
                model.APIKey_OverrideForStore = _settingService.SettingExists(iyzicoPaymentSettings, x => x.APIKey, storeScope);
                model.SecretKey_OverrideForStore = _settingService.SettingExists(iyzicoPaymentSettings, x => x.SecretKey, storeScope);

                
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(iyzicoPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(iyzicoPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.ShippableProductRequired_OverrideForStore = _settingService.SettingExists(iyzicoPaymentSettings, x => x.ShippableProductRequired, storeScope);
            }

            return View("~/Plugins/Payments.Iyzico/Views/PaymentIyzico/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area("Admin")]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var iyzicoPaymentSettings = _settingService.LoadSetting<IyzicoPaymentSettings>(storeScope);

            //save settings
            iyzicoPaymentSettings.DescriptionText = model.DescriptionText;            
            iyzicoPaymentSettings.APIUrl = model.APIUrl;
            iyzicoPaymentSettings.APIKey = model.APIKey;
            iyzicoPaymentSettings.SecretKey = model.SecretKey;

            iyzicoPaymentSettings.AdditionalFee = model.AdditionalFee;
            iyzicoPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            iyzicoPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.DescriptionText_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(iyzicoPaymentSettings, x => x.DescriptionText, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(iyzicoPaymentSettings, x => x.DescriptionText, storeScope);

            if (model.APIUrl_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(iyzicoPaymentSettings, x => x.APIUrl, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(iyzicoPaymentSettings, x => x.APIUrl, storeScope);
            
            if (model.APIKey_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(iyzicoPaymentSettings, x => x.APIKey, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(iyzicoPaymentSettings, x => x.APIKey, storeScope);
                        
            if (model.SecretKey_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(iyzicoPaymentSettings, x => x.SecretKey, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(iyzicoPaymentSettings, x => x.SecretKey, storeScope);

            if (model.AdditionalFee_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(iyzicoPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(iyzicoPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(iyzicoPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(iyzicoPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            if (model.ShippableProductRequired_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(iyzicoPaymentSettings, x => x.ShippableProductRequired, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(iyzicoPaymentSettings, x => x.ShippableProductRequired, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
        
       
    }
}
