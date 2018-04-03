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

		public T Task { get; internal set; }

		public Task<Result> WhenAcknowledged
			=> syncAcknowledged.Task;

		public bool IsFailed
		{
			get
			{
				switch (Task.Progress)
				{
					case TaskProgress.Timeout:
					case TaskProgress.Failed:
					case TaskProgress.Cancelled:
						return true;

					default:
						return false;
				}
			}
		}

		public override string ToString()
			=> $"Flow | {Task}";

		internal void Update(T task)
		{
		}

		internal void Update(TaskProgress progress)
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

					syncCompleted.TrySetResult(true);

					break;

				case TaskProgress.Timeout:

					syncAcknowledged.TrySetResult(Result.Timeout);

					syncCompleted.TrySetResult(Result.Timeout);

					break;

				case TaskProgress.Failed:

					syncAcknowledged.TrySetResult(Result.Error());

					syncCompleted.TrySetResult(Result.Error());

					break;

				case TaskProgress.Cancelled:

					syncAcknowledged.TrySetResult(true);

					syncCompleted.TrySetResult(Result.Cancelled);

					break;
			}
		}
	}

	public class ProtectedTaskflow : Taskflow<ProtectedTask>
	{
		private static Dictionary<string, ProtectedTaskflow> pending = new Dictionary<string, ProtectedTaskflow>();

		public ProtectedTaskflow(ProtectedTask task) : base(task)
		{
		}

		public static int NumPending
					=> pending.Count;

		internal static void Update(string operationID, TaskProgress progress)
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

		internal void SetPending()
		{
			pending.Add(Task.OperationID, this);
		}
	}

	public class ProtectedTaskflow<T> : ProtectedTaskflow where T : ProtectedTask
	{
		public ProtectedTaskflow(T task) : base(task)
		{
		}

		new public T Task
		{
			get => base.Task as T;

			internal set { base.Task = value; }
		}
	}
}