using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace EmnExtensions.MathHelpers
{
	[Serializable]
	public class MatrixMismatchException:Exception
	{
		public MatrixMismatchException() : base() { }
		public MatrixMismatchException(string message) : base(message) { }
		public MatrixMismatchException(string message, Exception innerException) : base(message, innerException) { }
		protected MatrixMismatchException(SerializationInfo info, StreamingContext context):base(info,context){}
	}
}
