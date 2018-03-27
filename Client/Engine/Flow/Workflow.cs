﻿using System.Threading.Tasks;

namespace SLD.Tezos.Client.Flow
{
	public class Workflow : TezosObject
	{
		protected volatile TaskCompletionSource<Result> syncCompleted = new TaskCompletionSource<Result>();

		public Task<Result> WhenCompleted
			=> syncCompleted.Task;
	}
}