namespace SLD.Tezos.Blockchain
{
	using Protocol;
	using System;

	public class Block
	{
		public BaseTask[] Operations;

		public int Index;
		public DateTime Time;
	}
}