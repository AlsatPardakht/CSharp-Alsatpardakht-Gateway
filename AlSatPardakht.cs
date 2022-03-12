using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json; // Solution Explorer->Right Click on Project Name -> Click on Manage Nuget Packages -> Search for newtonsoft -> Click on install button 

namespace AlsatPardakht
{
	public class AlSatPardakht 
	{
		#region Declaring and Initializing Variables
		// *********************************** //  Mostaghim \\ ***********************************
		private static readonly string MostaghimApi = ""; // Mostaghim IPG API - Get it from AlSatPardakht.com
		private static readonly string MostaghimUrlForVerifyApi = "https://www.alsatpardakht.com/API_V1/sign.php"; // Please Do Not Change
		private static readonly string MostaghimUrlForPaymanet = "https://www.alsatpardakht.com/API_V1/Go.php?Token="; // Please Do Not Change
		private static readonly string MostaghimVerifyPaymenyUrl = "https://www.alsatpardakht.com/API_V1/callback.php"; // Please Do Not Change
		private static readonly string MostaghimRedirectAddress = "https://YourSite.com/..../...."; // General Redirect Address

		// *********************************** //  Vaset  \\ ***********************************
		private static readonly string VasetApi = ""; // Vaset IPG API - Get it from AlSatPardakht.com

		private static readonly string VasetUrlForVerifyApi = "https://www.alsatpardakht.com/IPGAPI/Api22/send.php"; // Please Do Not Change
		private static readonly string VasetUrlForPaymanet = "https://www.alsatpardakht.com/IPGAPI/Api2/Go.php?Token="; // Please Do Not Change
		private static readonly string VasetVerifyPaymenyUrl = "https://www.alsatpardakht.com/IPGAPI/Api22/VerifyTransaction.php"; // Please Do Not Change

		private static readonly string VasetRedirectAddress = "https://YourSite.com/..../...."; // General Redirect Address

		#endregion

		#region Methods

		//Send Http Request To AlSatPardakht 
		public static HttpWebResponse HttpRequestToAlSat(string url, string data)
		{
			var httpWebRequest = (HttpWebRequest)WebRequest.Create(url); // make request
			httpWebRequest.ContentType = "application/x-www-form-urlencoded"; // content of request -> must form data
			httpWebRequest.Method = "POST"; // method of request -> must be POST
			using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
			{
				streamWriter.Write(data); // send request
				streamWriter.Flush(); // flush stream
			}
			return (HttpWebResponse)httpWebRequest.GetResponse(); // get Response
		}

		//Step 1: Send Http Request To AlSatPardakhtt For Verify API (Send Invoice Info)
		// If Send Data Currectly And Api Is Trust, Get: IsSuccess = 1 And Token != null => Go To Step 2
		public static ResponseHttpRequest HttpRequestToAlSatForVerify(MakeRequest makeRequest, PaymentMethod method)
		{
			string Api, Url = "", RedirectAddress, Data = "";
			if (method == PaymentMethod.Mostaghim)
			{
				Api = MostaghimApi;
				Url = MostaghimUrlForVerifyApi;
				if (!string.IsNullOrEmpty(makeRequest.RedirectAddress))
					RedirectAddress = makeRequest.RedirectAddress;
				else
					RedirectAddress = MostaghimRedirectAddress;
				Data = "Api=" + Api + "&" + "Amount=" + makeRequest.Amount + "&" + "RedirectAddress=" + RedirectAddress + "&" + "InvoiceNumber=" + makeRequest.InvoiceNumber;
			}
			else if (method == PaymentMethod.Vaset)
			{
				Api = VasetApi;
				Url = VasetUrlForVerifyApi;
				if (!string.IsNullOrEmpty(makeRequest.RedirectAddress))
					RedirectAddress = makeRequest.RedirectAddress;
				else
					RedirectAddress = VasetRedirectAddress;
				Data = "ApiKey=" + Api + "&" + "Amount=" + makeRequest.Amount + "&" + "RedirectAddressPage=" + RedirectAddress + "&" + "Tashim=" + JsonConvert.SerializeObject(makeRequest.Tashims) + "&" + "PayId=" + makeRequest.InvoiceNumber;
			}
			try
			{
				ResponseHttpRequest result = new ResponseHttpRequest();

				var httpResponse = HttpRequestToAlSat(Url, Data);  // get Response 
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) // make stream reader
				{
					var responseText = streamReader.ReadToEnd(); // read Response
					result = JsonConvert.DeserializeObject<ResponseHttpRequest>(responseText);
				}
				return result;
			}
			catch
			{
				return new ResponseHttpRequest();
			}
		}

		//Step2: Get Token Form Step 1 And Redirect User To Payment Page (MostaghimUrlForPaymanet Or VasetUrlForPaymanet),
		//After Payment User Will Redirected To You Site Via "MostagimRedirectAddress" Or "VasetRedirectAddress" And Go To Step 3
		public static string RedirectUserToPaymentPage(string token, PaymentMethod method)
		{
			if (method == PaymentMethod.Mostaghim)
				return (MostaghimUrlForPaymanet + token);
			else if (method == PaymentMethod.Vaset)
				return (VasetUrlForPaymanet + token);
			else
				return null;
		}

		//Step 3: Get "tref", "iD", "iN"  Send Again For Verify Payment And Get Payment Information
		public static GetPaymentInfo VerifyPayment(string tref, string iN, string iD, int amount, PaymentMethod method)
		{
			string Api, VerifyPaymenyUrl="", Data=""; 
			if (method == PaymentMethod.Mostaghim)
			{
				Api = MostaghimApi;
				VerifyPaymenyUrl = MostaghimVerifyPaymenyUrl;
				Data = "Api=" + Api + "&" + "tref=" + tref + "&" + "InvoiceNumber=" + iN + "&" + "InvoiceDate=" + iD;
			}
			else if (method == PaymentMethod.Vaset)
			{
				Api = VasetApi;
				VerifyPaymenyUrl = VasetVerifyPaymenyUrl;
				Data = "ApiKey=" + Api + "&" + "tref=" + tref + "&" + "iN=" + iN + "&" + "iD=" + iD;
			}
			try
			{
				GetPaymentInfo result = new GetPaymentInfo();
				var httpResponse = HttpRequestToAlSat(VerifyPaymenyUrl, Data);  // get Response
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) // make stream reader
				{
					var responseText = streamReader.ReadToEnd(); // read Response
					try
					{
						result = JsonConvert.DeserializeObject<GetPaymentInfo>(responseText);
						if (result != null)
						{
							if (result.Verify.IsSuccess == true && result.PSP.IsSuccess == true && result.PSP.Amount == amount)
								return result;
							else
								return new GetPaymentInfo();
						}
						else
							return new GetPaymentInfo();
					}
					catch
					{
						return new GetPaymentInfo();
					}
				}
			}
			catch
			{
				return new GetPaymentInfo();
			}
		}

		#endregion

		#region Classes 
		public class MakeRequest
		{
			public int Amount { get; set; }
			public string InvoiceNumber { get; set; }
			public string RedirectAddress { get; set; } // User Site
			public List<Tashim> Tashims { get; set; } 
		}
		public class ResponseHttpRequest
		{
			public string IsSuccess { get; set; }
			public string TimeStamp { get; set; }
			public string InvoiceDate { get; set; }
			public string Token { get; set; }
			public string Sign { get; set; } 
		}
		public class PaymentRequestResult
		{
			public string tref { get; set; }
			public string iN { get; set; }
			public string iD { get; set; }
		}
		public class GetPaymentInfo
		{
			public PSP PSP { get; set; }
			public Verify Verify { get; set; }
		}
		public class PSP
		{
			public int TraceNumber { get; set; }
			public long ReferenceNumber { get; set; }
			public string TransactionDate { get; set; }
			public string TransactionReferenceID { get; set; }
			public string InvoiceNumber { get; set; }
			public string InvoiceDate { get; set; }
			public int MerchantCode { get; set; }
			public int TerminalCode { get; set; }
			public int Amount { get; set; }
			public string TrxHashedCardNumber { get; set; }
			public string TrxMaskedCardNumber { get; set; }
			public bool IsSuccess { get; set; }
			public string Message { get; set; }
		}
		public class Verify
		{
			public string MaskedCardNumber { get; set; }
			public string HashedCardNumber { get; set; }
			public string ShaparakRefNumber { get; set; }
			public bool IsSuccess { get; set; }
			public string Message { get; set; }

		}
		public class Tashim
		{
			public string Name { get; set; }
			public string Family { get; set; }
			public string CodeMelli { get; set; }
			public string Shaba { get; set; }
			public string Price { get; set; }
		}
		public enum PaymentMethod
		{
			Mostaghim,          // IPG  مستقیم
			Vaset        // IPG  واسط 
		}
		#endregion

	}
}
