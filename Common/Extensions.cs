using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SLD.Tezos
{
	public static class Extensions
	{
		#region Byte Arrays

		public static string ToHexString(this byte[] data)
		{
			StringBuilder hex = new StringBuilder(data.Length * 2);

			foreach (byte b in data)
			{
				hex.AppendFormat("{0:x2}", b);
			}

			return hex.ToString();
		}

		public static byte[] HexToByteArray(this string hex)
		{
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}

		#endregion Byte Arrays

		#region Serialization

		public static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.Objects,
			MissingMemberHandling = MissingMemberHandling.Ignore,
		});

		public static T ToModelObject<T>(this string json) where T : class
		{
			if (string.IsNullOrEmpty(json))
			{
				return null;
			}

			using (var inner = new StringReader(json))
			using (var outer = new JsonTextReader(inner))
			{
				return Serializer.Deserialize<T>(outer);
			}
		}

		public static string ToJson(this object source)
		{
			using (var writer = new StringWriter())
			{
				Serializer.Serialize(writer, source);

				return writer.ToString();
			}
		}

		#endregion Serialization

		#region Safe strings

		public static string ToSafeString(this DateTime time)
			=> time.ToString("u");

		public static string ToSafeString(this TimeSpan timespan)
			=> timespan.ToString("c");

		#endregion Safe strings
	}
}