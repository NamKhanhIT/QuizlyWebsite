using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace QuizlyWebsite.Services
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(int orderId, string orderCode, decimal amount, string orderDescription, string? customerName, string? customerEmail, string? customerPhone, string ipAddress, string? returnUrl = null, string paymentMethod = "VNBANK");
        VNPayPaymentResponse ProcessPaymentResponse(Dictionary<string, string> queryParams);
    }

    public class VNPayService : IVNPayService
    {
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _paymentUrl;
        private readonly string _baseReturnUrl;

        public VNPayService(string tmnCode, string hashSecret, string paymentUrl, string baseReturnUrl)
        {
            _tmnCode = tmnCode;
            _hashSecret = hashSecret;
            _paymentUrl = paymentUrl;
            _baseReturnUrl = baseReturnUrl;
        }

        public string CreatePaymentUrl(int orderId, string orderCode, decimal amount, string orderDescription, string? customerName, string? customerEmail, string? customerPhone, string ipAddress, string? returnUrl = null, string paymentMethod = "VNBANK")
        {
            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _tmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_TxnRef", orderCode);
            vnpay.AddRequestData("vnp_OrderInfo", orderDescription);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl ?? _baseReturnUrl);
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

            // Thêm thông tin khách hàng
            if (!string.IsNullOrEmpty(customerName))
                vnpay.AddRequestData("vnp_Bill_FirstName", customerName);
            if (!string.IsNullOrEmpty(customerEmail))
                vnpay.AddRequestData("vnp_Bill_Email", customerEmail);
            if (!string.IsNullOrEmpty(customerPhone))
                vnpay.AddRequestData("vnp_Bill_Mobile", customerPhone);

            // Chọn phương thức thanh toán
            if (paymentMethod == "VNPAYQR")
            {
                vnpay.AddRequestData("vnp_BankCode", "");
            }
            else if (paymentMethod == "VNBANK")
            {
                // Để trống để khách hàng chọn ngân hàng
                vnpay.AddRequestData("vnp_BankCode", "");
            }

            string paymentUrl = vnpay.CreateRequestUrl(_paymentUrl, _hashSecret);
            return paymentUrl;
        }

        public VNPayPaymentResponse ProcessPaymentResponse(Dictionary<string, string> queryParams)
        {
            var vnpay = new VnPayLibrary();
            foreach (var param in queryParams)
            {
                if (!string.IsNullOrEmpty(param.Value))
                {
                    vnpay.AddResponseData(param.Key, param.Value);
                }
            }

            var orderId = vnpay.GetResponseData("vnp_TxnRef");
            var vnpayTranId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_SecureHash = queryParams.FirstOrDefault(x => x.Key == "vnp_SecureHash").Value;
            var orderInfo = vnpay.GetResponseData("vnp_OrderInfo");
            var amount = Convert.ToDecimal(vnpay.GetResponseData("vnp_Amount")) / 100;
            var bankCode = vnpay.GetResponseData("vnp_BankCode");
            var payDate = vnpay.GetResponseData("vnp_PayDate");

            var checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _hashSecret);

            return new VNPayPaymentResponse
            {
                Success = checkSignature && vnp_ResponseCode == "00",
                OrderId = orderId,
                TransactionId = vnpayTranId,
                Amount = amount,
                OrderDescription = orderInfo,
                BankCode = bankCode,
                PayDate = payDate,
                ResponseCode = vnp_ResponseCode
            };
        }
    }

    public class VNPayPaymentResponse
    {
        public bool Success { get; set; }
        public string? OrderId { get; set; }
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string? OrderDescription { get; set; }
        public string? BankCode { get; set; }
        public string? PayDate { get; set; }
        public string? ResponseCode { get; set; }
    }

    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            var data = new StringBuilder();
            foreach (var kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            var queryString = data.ToString();
            baseUrl += "?" + queryString;
            var signData = queryString;
            if (signData.Length > 0)
            {
                signData = signData.Remove(data.Length - 1, 1);
            }

            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rspRaw = GetResponseData();
            var myChecksum = HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }

            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }

            foreach (var kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}

