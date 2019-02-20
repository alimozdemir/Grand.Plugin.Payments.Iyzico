using System.ComponentModel.DataAnnotations;

namespace Grand.Plugin.Payments.PayPalStandard.Models
{
    public class Secure3DHandler
    {
        [Required]
        public string status { get; set; }
        [Required]
        public string paymentId { get; set; }
        public string conversationData { get; set; }
        [Required]
        public string conversationId { get; set; }
        public string mdStatus { get; set; }
    }
}