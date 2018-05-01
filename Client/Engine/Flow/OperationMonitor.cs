using System;
using System.Threading;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Flow
{
	using Protocol;
	using Connections;

	public class OperationMonitor : TezosObject
	{
		private OperationTaskflow flow;
		Action<OperationEvent> callback;
		private CancellationTokenSource cancelSource = new CancellationTokenSource();
		IConnection connection;
		public bool IsComplete;

		public OperationMonitor(
			OperationTaskflow flow,
			TimeSpan timeoutAcknowledge,
			TimeSpan timeoutComplete,
			IConnection connection,
			Action<OperationEvent> callback)
		{
			this.flow = flow;
			this.callback = callback;
			this.connection = connection;
			TimeoutAcknowledge = timeoutAcknowledge;
			TimeoutComplete = timeoutComplete;

			Monitor();
		}

		private TimeSpan TimeoutAcknowledge, TimeoutComplete;

		public override string ToString()
			=> $"Mon | {flow.Task.OperationID}";

		public void Update(TaskProgress progress)
		{
			// Only when progress changed, flows might be updated by more than one message
			if (progress != flow.Task.Progress)
			{
				flow.Update(progress);

				cancelSource.Cancel(); 
			}
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

					if (flow.IsAcknowledged) continue;

					Trace("Timeout for acknowledge");

					retryAfter = await GetNextEvent(flow);
				}
				catch(TaskCanceledException)
				{
					continue;
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

					if (flow.IsComplete) continue;

					Trace("Timeout for completion");

					retryAfter = await GetNextEvent(flow);
				}
				catch (TaskCanceledException)
				{
					continue;
				}
				catch
				{
					// Most likely cancelled, but any expection shall continue
				}
			}

			IsComplete = true;
		}

		private async Task<TimeSpan> GetNextEvent(OperationTaskflow flow)
		{
			var operationStatus = await connection.GetOperationStatus(flow.Task);

			foreach (var operationEvent in operationStatus.Events)
			{
				callback(operationEvent);
			}

			return operationStatus.RetryAfter;
		}

	}
}