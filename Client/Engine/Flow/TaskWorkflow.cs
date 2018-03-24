using System.Collections.Generic;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Flow
{
	using Protocol;

	public class Taskflow<T> : Workflow where T : BaseTask
	{
		protected volatile TaskCompletionSource<Result> syncAcknowledged = new TaskCompletionSource<Result>();

		public Taskflow(T task)
		{
			this.Task = task;
		}

		public T Task { get; private set; }

		public Task<Result> WhenAcknowledged
			=> syncAcknowledged.Task;

		public void Update(TaskProgress progress)
		{
			Trace($"Update progress: {progress}");

			Task.Progress = progress;

			switch (progress)
			{
				case TaskProgress.Acknowledged:

					syncAcknowledged.TrySetResult(true);

					break;

				case TaskProgress.Confirmed:

					syncAcknowledged.TrySetResult(true);

					syncCompleted.SetResult(true);

					break;

				case TaskProgress.Timeout:

					syncAcknowledged.TrySetResult(Result.Timeout);

					syncCompleted.SetResult(Result.Timeout);

					break;

				case TaskProgress.Failed:

					syncAcknowledged.TrySetResult(Result.Error());

					syncCompleted.SetResult(Result.Error());

					break;
			}
		}

		public override string ToString()
			=> $"Flow | {Task}";
	}

	public class ProtectedTaskflow : Taskflow<ProtectedTask>
	{
		private static Dictionary<string, ProtectedTaskflow> pending = new Dictionary<string, ProtectedTaskflow>();

		public ProtectedTaskflow(ProtectedTask task) : base(task)
		{
			pending.Add(task.OperationID, this);
		}

		public static int NumPending
					=> pending.Count;

		public static void Update(string operationID, TaskProgress progress)
		{
			if (pending.TryGetValue(operationID, out ProtectedTaskflow flow))
			{
				if (progress.IsFinal())
				{
					pending.Remove(operationID);
				}

				flow.Update(progress);
			}
		}
	}
}