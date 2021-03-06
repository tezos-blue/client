﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SLD.Tezos
{
	using Protocol;

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

		#region Safe strings

		public static string ToSafeString(this DateTime time)
			=> time.ToString("u");

		public static string ToSafeString(this TimeSpan timespan)
			=> timespan.ToString("c");

		public static string ToSafeString(this decimal amount)
			=> amount.ToString("g", CultureInfo.InvariantCulture);

		public static decimal ToDecimal(this string amount)
			=> amount == null ? 0 : decimal.Parse(amount, CultureInfo.InvariantCulture);

		#endregion Safe strings

		#region Flow

		public static bool IsFinal(this TaskProgress progress)
		{
			switch (progress)
			{
				case TaskProgress.Confirmed:
				case TaskProgress.Timeout:
				case TaskProgress.Failed:
					return true;

				default:
					return false;
			}
		}

		#endregion Flow

		#region Linq

		public static int IndexOf<T>(this IEnumerable<T> enumerable, T item)
		{
			if (enumerable == null || item == null)
			{
				return -1;
			}

			int i = 0;

			foreach (var found in enumerable)
			{
				if (ReferenceEquals(found, item))
				{
					return i;
				}

				i++;
			}

			return -1;
		}

		#endregion Linq
	}
}