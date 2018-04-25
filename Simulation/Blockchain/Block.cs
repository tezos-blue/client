using System;

namespace SLD.Tezos.Blockchain
{
	using Protocol;

	public class Block
	{
		public int Index;
		public DateTime Time;

		public OperationTask[] Operations;
	}
}