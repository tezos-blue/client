using System;

namespace SLD.Tezos
{
	public enum ServerError
	{
		Unexpected,
		AccountNotFound,
	}

	public class ErrorResponse
	{
		public string Message;

		public ServerError Error;

		public string Verbose;

		public ErrorResponse()
		{
		}

		public ErrorResponse(Exception e)
		{
			Message = e.Message;
			Verbose = Tracer.CreateExceptionText(e);

			if (e is ServerException se)
			{
				Error = se.ServerError;
			}
		}
	}

	public class ServerException : Exception, ITraceException
	{
		public ServerException(ServerError error, string message, Exception inner) : base(message, inner)
		{
			ServerError = error;
		}

		public ServerError ServerError { get; private set; }

		public void WriteTrace(TezosTraceWriter writer)
		{
			writer.WriteProperty("Error", ServerError);
		}

		private static string MakeMessage(string message, ServerError error)
		{
			return $"{message} | {error}";
		}
	}
}