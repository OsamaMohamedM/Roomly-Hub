using Application.DTOs.Payment;
using Application.DTOs.Payment.FawaterkRequest;
using Application.Interfaces.Services;
using Domain.enums.Booking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using PaymentMethoodModel = Domain.Entities.Payment.PaymentMethoodModel;

namespace Infrastructure.Services.Payment
{
    public class FawaterakPaymentService : IPaymentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FawaterakPaymentService> _logger;
        private readonly string ApiKey;
        private readonly string BaseUrl;
        private readonly string ProviderKey;

        public FawaterakPaymentService(
            IHttpClientFactory httpClientFactory,
            IOptions<FawaterakOptions> options,
            ILogger<FawaterakPaymentService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            var cfg = options.Value;
            ApiKey = cfg.ApiKey;
            BaseUrl = cfg.BaseUrl;
            ProviderKey = cfg.ProviderKey;
        }

        public async Task<EInvoiceResponseData?> CreateEInvoiceAsync(EInvoiceRequestModel eInvoice)
        {
            try
            {
                if (eInvoice == null)
                    return null;

                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/createInvoiceLink");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                request.Content = new StringContent(JsonConvert.SerializeObject(eInvoice), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                if (response == null)
                    return null;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var eInvoiceResponse = JsonConvert.DeserializeObject<EInvoiceResponseData>(responseContent);
                    return eInvoiceResponse;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Fawaterak invoice.");
                return null;
            }
        }

        public async Task<IList<PaymentMethoodModel>?> GetPaymentMethods()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/getPaymentmethods");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

                var result = await client.SendAsync(request);

                if (result.IsSuccessStatusCode)
                {
                    var responseContent = await result.Content.ReadAsStringAsync();
                    var paymentMethodsResponse = JsonConvert.DeserializeObject<PaymentMethodsResponse>(responseContent);

                    if (paymentMethodsResponse?.Data != null)
                    {
                        foreach (var item in paymentMethodsResponse.Data)
                        {
                            item.Id = item.PaymentId;
                        }
                    }

                    return paymentMethodsResponse?.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Fawaterak payment methods.");
                return null;
            }
        }

        public async Task<PaymentMethod> GetPaymentMethod(int paymentMethodId, IList<PaymentMethoodModel>? paymentMethods = null)
        {
            try
            {
                var methods = paymentMethods ?? await GetPaymentMethods();

                var method = methods?.FirstOrDefault(x => x.PaymentId == paymentMethodId);
                if (method == null || string.IsNullOrWhiteSpace(method.NameEn))
                    return PaymentMethod.Card;

                var name = method.NameEn;

                if (name.Contains("Fawry", StringComparison.OrdinalIgnoreCase))
                    return PaymentMethod.Fawry;

                if (name.Contains("Meeza", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Wallet", StringComparison.OrdinalIgnoreCase))
                    return PaymentMethod.Ewallet;

                return PaymentMethod.Card;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map Fawaterak payment method {PaymentMethodId}.", paymentMethodId);
                return PaymentMethod.Card;
            }
        }

        public async Task<BasePaymentResponse?> GeneralPay(EInvoiceRequestModel invoice)
        {
            try
            {
                if (invoice?.PaymentMethodId == null)
                    return null;

                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/invoiceInitPay");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                request.Content = new StringContent(JsonConvert.SerializeObject(invoice), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var method = await GetPaymentMethod(invoice.PaymentMethodId.Value);

                    return method switch
                    {
                        PaymentMethod.Fawry => JsonConvert.DeserializeObject<FawryPaymentResponse>(responseContent),
                        PaymentMethod.Ewallet => JsonConvert.DeserializeObject<MeezaPaymentResponse>(responseContent),
                        PaymentMethod.Card => JsonConvert.DeserializeObject<CardPaymentResponse>(responseContent),
                        _ => null
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Fawaterak payment.");
                return null;
            }
        }

        public bool VerifyWebhook(WebHookModel webHook)
        {
            try
            {
                if (webHook == null)
                    return false;

                var generatedHashKey = GenerateHashKeyForWebhookVerification(
                    webHook.InvoiceId,
                    webHook.InvoiceKey,
                    webHook.PaymentMethod);

                return generatedHashKey == webHook.HashKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify Fawaterak webhook.");
                return false;
            }
        }

        public bool VerifyCancelTransaction(CancelTransactionModel cancelTransaction)
        {
            try
            {
                if (cancelTransaction == null)
                    return false;

                var generatedHashKey = GenerateHashKeyForCancelTransaction(
                    cancelTransaction.ReferenceId,
                    cancelTransaction.PaymentMethod);

                return generatedHashKey == cancelTransaction.HashKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify Fawaterak cancellation.");
                return false;
            }
        }

        public bool VerifyApiKeyTransaction(string apiKey)
        {
            return !string.IsNullOrEmpty(apiKey) && apiKey == ApiKey;
        }

        public string GenerateHashKeyForIFrame(string domain)
        {
            var queryParam = $"Domain={domain}&ProviderKey={ProviderKey}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryParam));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private string GenerateHashKeyForWebhookVerification(long invoiceId, string invoiceKey, string paymentMethod)
        {
            var queryParam = $"InvoiceId={invoiceId}&InvoiceKey={invoiceKey}&PaymentMethod={paymentMethod}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryParam));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private string GenerateHashKeyForCancelTransaction(string referenceId, string paymentMethod)
        {
            var queryParam = $"referenceId={referenceId}&PaymentMethod={paymentMethod}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryParam));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private static string NormalizeEmailDots(string email)
        {
            if (string.IsNullOrEmpty(email))
                return email;

            int atIndex = email.IndexOf('@');
            if (atIndex < 0)
                return email;

            string localPart = email.Substring(0, atIndex);
            string domainPart = email.Substring(atIndex);

            localPart = System.Text.RegularExpressions.Regex.Replace(localPart, @"\.+", ".");

            return localPart + domainPart;
        }
    }
}