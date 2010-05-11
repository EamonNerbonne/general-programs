using System;
using System.Linq;
using System.Net.Sockets;
using System.Web;

namespace HttpHeaderHelper
{
	public enum HttpMethod
	{
		OPTIONS, GET, HEAD, POST, PUT, DELETE, TRACE, CONNECT, InvalidMethod
	}


	public class HttpRequestHelper
	{
		HttpContext context;
		HttpMethod method;
		IHttpRequestProcessor reqProc;
		ResourceInfo resource;
		bool requestFinished;
		bool canAcceptRangeRequest = false;
		Range[] requestedRanges = null;
		//		string statusErrorMessageBody;

		public HttpContext Context { get { return context; } }

		public bool AssertMethodIsOneOf(params HttpMethod[] acceptedMethods) {
			bool isOK = acceptedMethods.Contains(method);
			if(!isOK) {
				SetFinalStatus(501, "Http Method Not Supported");
			}
			return isOK;
			
		}

		public static string encode(string str) { return HttpUtility.UrlEncode(str); }
		public static string encode2(string str) { return Uri.EscapeDataString(str); }
		public static string encode3(string str) { return Uri.EscapeUriString(str); }
		public static string encode4(string str) { return HttpUtility.UrlPathEncode(str); }

		private void AssertNotYetFinished() {
			if(requestFinished) throw new Exception("This request has already been finished!");
		}
		public void SetFinalStatus(int statusCode, string statusDescription, string statusLongMessage) {
			AssertNotYetFinished();
			requestFinished = true;
			context.Response.StatusCode = statusCode;
			context.Response.StatusDescription = (statusDescription ?? "").Replace('\n', '\t');//safety net vs. Response Header Splitting.
			if(method != HttpMethod.HEAD) {
				context.Response.ContentType = "text/plain";
				context.Response.Write(statusLongMessage);
			}
		}
		public void SetFinalStatus(int statusCode) {
			SetFinalStatus(statusCode, null, null);
		}
		public void SetFinalStatus(int statusCode, string statusDescription) {
			SetFinalStatus(statusCode, statusDescription, null);
		}
		public void SetFinalStatus(ResourceError error) {
			SetFinalStatus(error.Code, error.ShortDescription, error.Message);
		}

		HttpMethod DetermineHttpMethod() {
			switch(context.Request.HttpMethod) {
				case "OPTIONS": return HttpMethod.OPTIONS;
				case "GET": return HttpMethod.GET;
				case "HEAD": return HttpMethod.HEAD;
				case "POST": return HttpMethod.POST;
				case "PUT": return HttpMethod.PUT;
				case "DELETE": return HttpMethod.DELETE;
				case "TRACE": return HttpMethod.TRACE;
				case "CONNECT": return HttpMethod.CONNECT;
				default: return HttpMethod.InvalidMethod;
			}
		}
		public HttpRequestHelper(HttpContext context) {
			this.context = context;
			this.method = DetermineHttpMethod();
			this.requestFinished = false;
		}

		public void Process(IHttpRequestProcessor reqProc) {
			this.reqProc = reqProc;

			Step1Initialize(); if(requestFinished) return;

			Step2aDetermineResource(); if(requestFinished) return;

			Step2bDetermineAcceptRange(); if(requestFinished) return;

			Step3SetCachingHeaders(); if(requestFinished) return;

			Step4Check304(); if(requestFinished) return;

			Step5Check412(); if(requestFinished) return;

			if(canAcceptRangeRequest) Step6DetermineRanges();
			if(requestFinished) return;


			if(requestedRanges != null) {//dealing with a range request
				Step7aPerformRangeRequests();
			} else {
				Step7bPerformNormalRequests();
			}

			if(!requestFinished) throw new Exception("Request isn't finished... Internal Error!");
		}


		private void Step1Initialize() {
			context.Response.ClearHeaders();
			context.Response.ClearContent();
			if(!AssertMethodIsOneOf(HttpMethod.GET, HttpMethod.HEAD)) return;

			reqProc.ProcessingStart();
		}

		private void Step2aDetermineResource() {
			PotentialResourceInfo tryResource = reqProc.DetermineResource() ?? new ResourceError();

			if(tryResource is ResourceError) {
				SetFinalStatus((ResourceError)tryResource);
			} else {
				resource = (ResourceInfo)tryResource;
			}
		}

		private void Step2bDetermineAcceptRange() {
			canAcceptRangeRequest = reqProc.SupportRangeRequests && resource.ResourceLength.HasValue;

			if(canAcceptRangeRequest) {
				context.Response.AppendHeader(HttpHeader.AcceptRanges, "bytes");
			}
		}

		private void Step3SetCachingHeaders() {
			HeaderUtility.SetPublicCache(context, resource);
			DateTime? expiresAt = reqProc.DetermineExpiryDate();
			if(expiresAt.HasValue) context.Response.Cache.SetExpires(expiresAt.Value.ToUniversalTime());
		}

		private void Step4Check304() {
			if(HeaderUtility.IsResponse304NotModified(context, resource)) SetFinalStatus(304);
		}

		private void Step5Check412() {
			if(HeaderUtility.IsResponse412PreconditionFailed(context, resource)) SetFinalStatus(412);
		}

		private void Step6DetermineRanges() {
			PreconditionStatus ifRange = HeaderUtility.PreConditionIfRange(context, resource);
			if(ifRange == PreconditionStatus.False) return;
			requestedRanges = HeaderUtility.ParseRangeHeader(context, (long)resource.ResourceLength);
			if(requestedRanges != null && requestedRanges.Length == 0) {//only invalid ranges...
				SetFinalStatus(416, "Requested range not satisfiable");
				requestedRanges = null;
			}
		}

		static void IgnoreDisconnectionExceptions(Action a) {
			try {
				a();
			} catch (SocketException) {
			} catch (System.Runtime.InteropServices.COMException) { //IIS7
			} catch (HttpException e) {
				if ((uint)e.ErrorCode != 0x800703E3 && (uint)e.ErrorCode !=0x800704CD) throw; //IIS7.5?
			}


		}

		private void Step7aPerformRangeRequests() {

			SetFinalStatus(206);//Successful Range Request

			if(requestedRanges.Length > 1) { //multipart!
				//TODO: implement
			}

			context.Response.AppendHeader(HttpHeader.ContentRange,
				"bytes " + requestedRanges[0].start + "-" + requestedRanges[0].lastByte + "/" + (long)resource.ResourceLength);
			context.Response.AppendHeader(HttpHeader.ContentLength,
				requestedRanges[0].length.ToString());
			if(resource.MimeType != null)
				context.Response.ContentType = resource.MimeType;


			if(method == HttpMethod.GET) {
				IgnoreDisconnectionExceptions(() => {
					reqProc.WriteByteRange(requestedRanges[0]); 
				});
			} else if(method == HttpMethod.HEAD) {
				//do nothing.
			} else {
				throw new Exception("Unsupported http method:" + method);
			}
		}

		private void Step7bPerformNormalRequests() {
			SetFinalStatus(200);//Successful Request
			if(resource.ResourceLength.HasValue)
				context.Response.AppendHeader(HttpHeader.ContentLength, resource.ResourceLength.Value.ToString());
			if(resource.MimeType != null)
				context.Response.ContentType = resource.MimeType;

			if(method == HttpMethod.GET) {
				IgnoreDisconnectionExceptions(() => {
					reqProc.WriteEntireContent();
				});
			} else if (method == HttpMethod.HEAD) {
				//do nothing
			} else {
				throw new Exception("Unsupported http method:" + method);
			}
		}
	}
}
