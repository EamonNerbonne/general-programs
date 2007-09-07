using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerializationTest
{
	class MyDataObj
	{
		string mmmData;
		public string MmmData { get { return mmmData; } set { mmmData = value; } }
	}

	public class MyDataPublicObj
	{
		string mmmData;
		public string MmmData { get { return mmmData; } set { mmmData = value; } }
	}

	class MyPublicDataObj
	{
		public string mmmData;
		public string MmmData { get { return mmmData; } set { mmmData = value; } }
	}

	class MyAutoDataObj
	{
		public string MmmData { get; set; }
	}


	class Program
	{
		static void Main(string[] args)
		{
			//TODO do serialization test...Serialize
		}
	}
}
