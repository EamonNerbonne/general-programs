
namespace HttpHeaderHelper
{
	public class HttpStatus
	{
		int status;
		string statusLine = null;
		string statusLongMessage = null;
		public HttpStatus(int statusCode) {
			status = statusCode;
		}
		public HttpStatus(int statusCode, string statusLine) {
			this.status = statusCode;
			this.statusLine = statusLine;
		}
		public HttpStatus(int statusCode, string statusLine, string statusLongMessage) {
			this.status = statusCode;
			this.statusLine = statusLine;
			this.statusLongMessage = statusLongMessage;
		}
	}
}
