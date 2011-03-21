using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace SongSearchSite {
	public static class JsonOutputExtension {
		public static void JsonOutput(this HttpResponse response, object toSerialize) {
			response.ContentType = "application/json";
			response.Output.Write(JsonConvert.SerializeObject(toSerialize));
		}
	}
}