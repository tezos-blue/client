using System;
using System.Collections.Generic;
using System.Threading;

namespace SLD.Tezos.Blockchain
{
	using Protocol;
	using Simulation;

	public class SimulatedBlockchain : TezosObject
	{
		private Timer pulse;
		private Stack<Block> blocks = new Stack<Block>();
		private List<OperationTask> pendingTasks = new List<OperationTask>();

		private int NextIndex = 0;

		public SimulatedBlockchain(SimulationParameters parameters)
		{
			Parameters = parameters;

			if (Parameters.AutoBlocks)
			{
				pulse = new Timer(OnPulse);
				Start();
			}
		}

		public event Action<Block> BlockCreated;

		public SimulationParameters Parameters { get; private set; }

		internal void Start()
		{
			var pulseSpan = Parameters.TimeBetweenBlocks;

			pulse.Change(pulseSpan, pulseSpan);
		}

		internal Block CreateBlock()
		{
			var index = NextIndex++;

			Trace($"Create Block {index}");

			var block = new Block
			{
				Index = index,
				Time = new DateTime(2000, 1, 1) + TimeSpan.FromSeconds(index),
				Operations = pendingTasks.ToArray(),
			};

			pendingTasks.Clear();

			blocks.Push(block);

			return block;
		}

		internal void Add(OperationTask task)
		{
			lock (pendingTasks)
			{
				task.Progress = TaskProgress.Submitted;
				pendingTasks.Add(task);
			}
		}

		private void OnPulse(object state)
		{
			lock (pendingTasks)
			{
				// Create new block
				Block block = CreateBlock();

				BlockCreated?.Invoke(block);
			}
		}
	}
}