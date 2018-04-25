using System.Collections.Generic;

namespace SLD.Tezos.Services
{
	using Protocol;

	public class Cache
	{
		private Dictionary<string, OperationInfo> operations = new Dictionary<string, OperationInfo>();

		internal void Store(OperationTask task, OperationEvent sourceEvent, OperationEvent destinationEvent)
		{
			operations[task.OperationID] = new OperationInfo
			{
				Progress = TaskProgress.Acknowledged,
				LastSourceEvent = sourceEvent,
				LastDestinationEvent = destinationEvent,
			};
		}

		internal void Update(OperationTask task, TaskProgress progress, OperationEvent sourceEvent, OperationEvent destinationEvent)
		{
			operations[task.OperationID] = new OperationInfo
			{
				Progress = progress,
				LastSourceEvent = sourceEvent,
				LastDestinationEvent = destinationEvent,
			};
		}

		internal OperationInfo GetOperation(string operationID)
			=> operations[operationID];

		public class OperationInfo
		{
			public OperationEvent LastSourceEvent;
			public OperationEvent LastDestinationEvent;
			public TaskProgress Progress;
		}
	}
}