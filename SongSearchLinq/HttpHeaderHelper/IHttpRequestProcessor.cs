using System;

namespace HttpHeaderHelper
{
	public interface IHttpRequestProcessor
	{
		void ProcessingStart();
		PotentialResourceInfo DetermineResource();
		DateTime? DetermineExpiryDate();
		/// <summary>
		/// Only relevant if the resource returned a resourceLength.
		/// </summary>
		bool SupportRangeRequests { get; }
		void WriteByteRange(Range range);
		void WriteEntireContent();
	}
}
