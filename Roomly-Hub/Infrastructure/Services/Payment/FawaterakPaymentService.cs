using Application.DTOs.Payment;
using Application.DTOs.Payment.FawaterkRequest;
using Application.Interfaces.Services;
using Domain.enums.Booking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
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

                EnsureInvoiceDefaults(eInvoice);

                _logger.LogInformation("Creating Fawaterak invoice for payment method {PaymentMethodId}", eInvoice.PaymentMethodId);

                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/createInvoiceLink");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                request.Content = new StringContent(JsonConvert.SerializeObject(eInvoice), Encoding.UTF8, "application/json");
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var wrappedResponse = JsonConvert.DeserializeObject<EInvoiceResponseModel>(responseContent);
                    var eInvoiceResponse = wrappedResponse?.Data;

                    if (eInvoiceResponse == null)
                    {
                        var token = JObject.Parse(responseContent);
                        var data = token["data"] ?? token;
                        eInvoiceResponse = new EInvoiceResponseData
                        {
                            InvoiceId = data["invoiceId"]?.ToString() ?? data["invoice_id"]?.ToString() ?? string.Empty,
                            InvoiceKey = data["invoiceKey"]?.ToString() ?? data["invoice_key"]?.ToString() ?? string.Empty,
                            Url = data["url"]?.ToString() ?? data["invoice_url"]?.ToString() ?? data["payment_url"]?.ToString() ?? data["redirectTo"]?.ToString() ?? string.Empty
                        };
                    }

                    _logger.LogInformation("Fawaterak invoice created successfully");
                    return eInvoiceResponse;
                }

                _logger.LogWarning("Fawaterak invoice creation failed with status code {StatusCode}", response.StatusCode);

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
                _logger.LogInformation("Loading payment methods from Fawaterak");
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

                _logger.LogWarning("Loading Fawaterak payment methods failed with status code {StatusCode}", result.StatusCode);

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
                _logger.LogInformation("Mapping payment method id {PaymentMethodId}", paymentMethodId);
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

                EnsureInvoiceDefaults(invoice);

                _logger.LogInformation("Processing general pay for payment method {PaymentMethodId}", invoice.PaymentMethodId);

                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/invoiceInitPay");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                request.Content = new StringContent(JsonConvert.SerializeObject(invoice), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var method = await GetPaymentMethod(invoice.PaymentMethodId.Value);

                    _logger.LogInformation("Fawaterak payment initialized successfully for method {Method}", method);

                    return method switch
                    {
                        PaymentMethod.Fawry => JsonConvert.DeserializeObject<FawryPaymentResponse>(responseContent),
                        PaymentMethod.Ewallet => JsonConvert.DeserializeObject<MeezaPaymentResponse>(responseContent),
                        PaymentMethod.Card => JsonConvert.DeserializeObject<CardPaymentResponse>(responseContent),
                        _ => null
                    };
                }

                _logger.LogWarning("Fawaterak general pay failed with status code {StatusCode}", response.StatusCode);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Fawaterak payment.");
                return null;
            }
        }

        private static void EnsureInvoiceDefaults(EInvoiceRequestModel invoice)
        {
            EnsureCurrency(invoice);
            EnsureCustomer(invoice);
            EnsureRedirectionUrls(invoice);
            EnsureCartItems(invoice);
            EnsureCartTotal(invoice);
        }

        private static void EnsureCurrency(EInvoiceRequestModel invoice)
        {
            if (string.IsNullOrWhiteSpace(invoice.Currency))
            {
                invoice.Currency = "EGP";
            }
        }

        private static void EnsureCustomer(EInvoiceRequestModel invoice)
        {
            invoice.Customer ??= new CustomerModel();
            invoice.Customer.FirstName ??= "Guest";
            invoice.Customer.LastName ??= "Guest";
            invoice.Customer.Email ??= "guest@roomly.com";
        }

        private static void EnsureRedirectionUrls(EInvoiceRequestModel invoice)
        {
            invoice.RedirectionUrls ??= new EInvoiceRedirectionUrls();
            invoice.RedirectionUrls.OnSuccess ??= "https://example.com/success";
            invoice.RedirectionUrls.OnFailure ??= "https://example.com/fail";
            invoice.RedirectionUrls.OnPending ??= "https://example.com/pending";
        }

        private static void EnsureCartItems(EInvoiceRequestModel invoice)
        {
            invoice.CartItems ??= new List<CartItemModel>();
            if (invoice.CartItems.Count == 0)
            {
                invoice.CartItems.Add(new CartItemModel
                {
                    Name = "Booking",
                    Price = "0.00",
                    Quantity = "1"
                });
            }

            foreach (var item in invoice.CartItems)
            {
                item.Name ??= "Booking";
                item.Price = NormalizeDecimalString(item.Price);
                item.Quantity = NormalizeQuantityString(item.Quantity);
            }
        }

        private static void EnsureCartTotal(EInvoiceRequestModel invoice)
        {
            if (string.IsNullOrWhiteSpace(invoice.CartTotal) || !decimal.TryParse(invoice.CartTotal, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                var total = invoice.CartItems.Sum(x =>
                {
                    var price = decimal.TryParse(x.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice) ? parsedPrice : 0m;
                    var quantity = decimal.TryParse(x.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedQuantity) ? parsedQuantity : 0m;
                    return price * quantity;
                });

                invoice.CartTotal = total.ToString("F2", CultureInfo.InvariantCulture);
            }
            else
            {
                invoice.CartTotal = decimal.Parse(invoice.CartTotal, NumberStyles.Any, CultureInfo.InvariantCulture).ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        private static string NormalizeDecimalString(string? value)
        {
            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                parsed = 0m;
            }

            return parsed.ToString("F2", CultureInfo.InvariantCulture);
        }

        private static string NormalizeQuantityString(string? value)
        {
            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
            {
                parsed = 1m;
            }

            return parsed.ToString("0", CultureInfo.InvariantCulture);
        }

        public bool VerifyWebhook(WebHookModel webHook)
        {
            try
            {
                if (webHook == null)
                    return false;

                _logger.LogInformation("Verifying webhook signature for invoice {InvoiceId}", webHook.InvoiceId);

                var generatedHashKey = GenerateHashKeyForWebhookVerification(
                    webHook.InvoiceId,
                    webHook.InvoiceKey,
                    webHook.PaymentMethod);

                var generatedBytes = Encoding.UTF8.GetBytes(generatedHashKey);
                var receivedBytes = Encoding.UTF8.GetBytes(webHook.HashKey.ToLowerInvariant());
                var isValid = generatedBytes.Length == receivedBytes.Length && CryptographicOperations.FixedTimeEquals(generatedBytes, receivedBytes);
                if (!isValid)
                {
                    _logger.LogWarning("Webhook signature mismatch for invoice {InvoiceId}", webHook.InvoiceId);
                }

                return isValid;
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

                _logger.LogInformation("Verifying cancellation signature for reference {ReferenceId}", cancelTransaction.ReferenceId);

                var generatedHashKey = GenerateHashKeyForCancelTransaction(
                    cancelTransaction.ReferenceId,
                    cancelTransaction.PaymentMethod);

                var isValid = generatedHashKey == cancelTransaction.HashKey;
                if (!isValid)
                {
                    _logger.LogWarning("Cancellation signature mismatch for reference {ReferenceId}", cancelTransaction.ReferenceId);
                }

                return isValid;
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
    }
}