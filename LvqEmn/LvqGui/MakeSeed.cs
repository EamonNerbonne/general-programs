using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Data;

namespace LvqGui
{
	public static class SeedMaker
	{
		static readonly RNGCryptoServiceProvider cryptGen = new RNGCryptoServiceProvider();
		public static uint[] MakeSeed()
		{
			var bytes = new byte[624 * sizeof(uint)];
			cryptGen.GetBytes(bytes);
			return ToUints(bytes);
		}

		public static uint[] ToUints(byte[] bytes)
		{
			var uintsAvailable = bytes.Length / sizeof(uint);
			uint[] retval = new uint[uintsAvailable];
			using (var ms = new MemoryStream(bytes))
			using (var br = new BinaryReader(ms))
				for (int i = 0; i < uintsAvailable; i++)
					retval[i] = br.ReadUInt32();
			return retval;
		}
		public static byte[] ToBytes(uint[] seed)
		{

			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				foreach (uint val in seed) bw.Write(val);
				bw.Flush();
				return ms.ToArray();
			}
		}
	}

	[ValueConversion(typeof(uint[]), typeof(string))]
	public class ByteArrayConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			if (targetType != typeof(string) || !(value is uint[]))
				throw new ArgumentException("Invalid input/output types: " + value.GetType().FullName + " -> " + targetType.FullName);

			uint[] seed = (uint[])value;

			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				foreach (uint val in seed) bw.Write(val);
				bw.Flush();
				return System.Convert.ToBase64String(ms.ToArray());
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			if (targetType != typeof(uint[]) || value.GetType() != typeof(string))
				throw new ArgumentException("Invalid input/output types: " + value.GetType().FullName + " -> " + targetType.FullName);

			var bytes = System.Convert.FromBase64String((string)value);
			var uintsAvailable = bytes.Length / sizeof(uint);
			uint[] retval = new uint[uintsAvailable];
			using (var ms = new MemoryStream())
			using (var br = new BinaryReader(ms))
				for (int i = 0; i < uintsAvailable; i++)
					retval[i] = br.ReadUInt32();
			return retval;
		}
	}
}
