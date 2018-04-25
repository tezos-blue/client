namespace SLD.Tezos
{
	/// <summary>
	/// A pseudo bool with capability to transport an error id, when failed.
	/// Alternative return value to indicate execution success or failure
	/// </summary>
	public struct Result
	{
		public const string GenericError = nameof(GenericError);

		public string ErrorID { get; private set; }

		public bool IsOK => !IsError;

		public bool IsError => ErrorID != null;

		#region Creation

		public static Result OK
			=> new Result();

		public static Result Timeout
			=> Error(nameof(Timeout));

		public static Result Cancelled
			=> Error(nameof(Cancelled));

		public static Result Error(string errorID = GenericError)
			=> new Result { ErrorID = errorID };

		#endregion Creation

		#region Boolean conversions

		public static implicit operator bool(Result result) => result.IsOK;

		public static implicit operator Result(bool result) => result ? OK : Error();

		#endregion Boolean conversions

		public override string ToString()
			=> IsOK ? "OK" : $"FAILED: {ErrorID}";
	}
}