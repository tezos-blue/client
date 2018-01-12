using System;
using System.IO;

namespace SLD.Tezos
{
	public class TezosTraceWriter : StringWriter
	{
		public void WriteProperty(string name, object value)
		{
			WriteLine($"{name,-15}: {value}");
		}

		public void WritePropertyNonNull(string name, object value)
		{
			if (value != null)
			{
				WriteProperty(name, value);
			}
		}

		public void WritePaddedLine(string text, char fill, int width)
		{
			var left = $"{text} ";

			var rest = Math.Max(0, width - left.Length);

			WriteLine(left + new string(fill, rest));
		}
	}
}