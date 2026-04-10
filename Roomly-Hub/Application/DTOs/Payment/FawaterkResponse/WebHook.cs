namespace Application.DTOs.Payment
{
    public class WebhookPayload
    {
        public string? OrderId { get; set; }
    }

    public class WebHookModel
    {
        public long InvoiceId { get; set; }
        public string InvoiceKey { get; set; }

        public string HashKey { get; set; }
        public string PaymentMethod { get; set; }

        public string InvoiceStatus { get; set; }

        public string? PayloadString { get; set; }
        public WebhookPayload? Payload { get; set; }
    }

    public class CancelTransactionModel
    {
        public string HashKey { get; set; }

        public string ReferenceId { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public object? PayLoad { get; set; }
    }

    public class FaliledWebHook
    {
        public long InvoiceId { get; set; }
        public string InvoiceKey { get; set; }
        public string ErrorMessage { get; set; }
    }
}