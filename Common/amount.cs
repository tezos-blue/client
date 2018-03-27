using System;

namespace SLD.Tezos
{
	public struct Amount
	{
		// special values
		public static Amount NaN = new Amount(decimal.MinValue);

		// representation
		private decimal value;

		private Amount(decimal value) : this()
		{
			this.value = value;
		}

		// conversion
		public static implicit operator decimal(Amount a)
		{
			if (a.value == NaN)
			{
				throw new ArgumentException("amount is NaN", nameof(a));
			}

			return a.value;
		}

		public static implicit operator Amount(decimal d)
		{
			if (d == decimal.MinValue)
			{
				throw new ArgumentException("amount is NaN", nameof(d));
			}

			return new Amount(d);
		}
	}
}