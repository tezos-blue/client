using System;
using System.Threading;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Flow
{
	using Protocol;

	internal class OperationMonitor : TezosObject
	{
		private OperationTaskflow flow;
		private Engine engine;

		private CancellationTokenSource cancelSource = new CancellationTokenSource();

		public OperationMonitor(OperationTaskflow flow, Engine engine)
		{
			this.flow = flow;
			this.engine = engine;

			Monitor();
		}

		private TimeSpan TimeoutAcknowledge
			=> engine.AcknowledgeTimeout;

		private TimeSpan TimeoutComplete
			=> engine.CompleteTimeout;

		public override string ToString()
			=> flow.Task.OperationID;

		internal void Update(TaskProgress progress)
		{
			flow.Update(progress);

			cancelSource.Cancel();
		}

		private async void Monitor()
		{
			Trace("Wait for acknowledge");

			var retryAfter = TimeoutAcknowledge;

			while (!flow.IsAcknowledged)
			{
				try
				{
					await Task.Delay(retryAfter, cancelSource.Token);

					Trace("Timeout for acknowledge");

					retryAfter = await GetNextEvent(flow);
				}
				catch
				{
					// Most likely cancelled, but any expection shall continue
				}
			}

			Trace("Wait for completion");

			cancelSource = new CancellationTokenSource();

			retryAfter = TimeoutComplete;

			while (!flow.IsComplete)
			{
				try
				{
					await Task.Delay(retryAfter, cancelSource.Token);

					Trace("Timeout for completion");

					retryAfter = await GetNextEvent(flow);
				}
				catch
				{
					// Most likely cancelled, but any expection shall continue
				}
			}
		}

		private async Task<TimeSpan> GetNextEvent(OperationTaskflow flow)
		{
			var operationStatus = await engine.Connection.GetOperationStatus(flow.Task);

			if (operationStatus.SourceEvent != null)
			{
				engine.InjectNetworkEvent(operationStatus.SourceEvent);
			}

			if (operationStatus.DestinationEvent != null)
			{
				engine.InjectNetworkEvent(operationStatus.DestinationEvent);
			}

			return operationStatus.RetryAfter;
		}
	}
}