using System.Collections.Generic;

namespace SLD.Tezos.Client.Flow
{
	using Protocol;

	public class Taskflow<T> : Workflow where T : BaseTask
	{
		public SyncEvent WhenAcknowledged = new SyncEvent();

		public Taskflow(T task)
		{
			this.Task = task;
		}

		public T Task { get; internal set; }

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

		public bool IsComplete
			=> Task.Progress == TaskProgress.Confirmed || IsFailed;

		public bool IsAcknowledged
			=> Task.Progress >= TaskProgress.Acknowledged;

		public override string ToString()
			=> $"Flow | {Task}";

		internal void Update(TaskProgress progress)
		{
			Trace($"Update progress: {progress}");

			Task.Progress = progress;

			switch (progress)
			{
				case TaskProgress.Acknowledged:

					WhenAcknowledged.SetComplete();

					break;

				case TaskProgress.Confirmed:

					WhenAcknowledged.SetComplete();

					WhenCompleted.SetComplete();

					break;

				case TaskProgress.Timeout:

					WhenAcknowledged.Timeout();

					WhenCompleted.Timeout();

					break;

				case TaskProgress.Failed:

					WhenAcknowledged.Fail();

					WhenCompleted.Fail();

					break;

				case TaskProgress.Cancelled:

					WhenAcknowledged.SetComplete();

					WhenCompleted.Cancel();

					break;
			}
		}
	}

	public class OperationTaskflow : Taskflow<OperationTask>
	{
		private static Dictionary<string, OperationMonitor> pending = new Dictionary<string, OperationMonitor>();

		public OperationTaskflow(OperationTask task) : base(task)
		{
		}

		public static int NumPending
					=> pending.Count;

		internal static void Update(string operationID, TaskProgress progress)
		{
			if (pending.TryGetValue(operationID, out OperationMonitor flow))
			{
				if (progress.IsFinal())
				{
					pending.Remove(operationID);
				}

				flow.Update(progress);
			}
		}

		internal void SetPending(Engine engine)
		{
			pending.Add(Task.OperationID, new OperationMonitor(this, engine));
		}
	}

	public class ProtectedTaskflow<T> : OperationTaskflow where T : ProtectedTask
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