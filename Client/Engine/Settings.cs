﻿using System;

namespace SLD.Tezos.Client
{
	partial class Engine
	{
		public const decimal DefaultOperationFee = 0.05M;
		public static TimeSpan TimeBetweenConnectionAttempts = TimeSpan.FromSeconds(15);
		public TimeSpan ApprovalTimeout { get; set; } = TimeSpan.FromMinutes(1);
	}
}