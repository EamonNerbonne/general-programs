using System.Web;
using HttpHeaderHelper;

namespace SongSearchSite.Code.Handlers
{
	/// <summary>
	/// Summary description for SongServeModule
	/// </summary>
	public class SongServeHandler : IHttpHandler {
		public bool IsReusable { get { return true; } }
		public void ProcessRequest(HttpContext context) {
			HttpRequestHelper helper = new HttpRequestHelper(context);
			SongServeRequestProcessor processor = new SongServeRequestProcessor(helper);
			helper.Process(processor);
		}

	}
}