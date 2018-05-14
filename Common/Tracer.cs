using System;
using System.Diagnostics;

namespace SLD.Tezos
{
	public interface ITraceException
	{
		void WriteTrace(TezosTraceWriter writer);
	}

	public static class Tracer
	{
		public static void Trace(object item, string text)
		{
			var id = item.ToString();

			Trace(text, id);
		}

		public static void Trace(Type type, string text)
		{
			Trace(text, type.Name);
		}

		public static void Trace(object tezosObject, Exception e)
		{
			Trace(tezosObject, CreateExceptionText(e));
		}

		public static void Trace(Type type, Exception e)
		{
			Trace(CreateExceptionText(e), type.Name);
		}

		public static string CreateExceptionText(Exception e)
		{
			var writer = new TezosTraceWriter();

			writer.WriteLine($"EXCEPTION");

			var current = e;

			while (e != null)
			{
				WriteException(e, writer);

				e = e.InnerException;
			}

			writer.WriteLine();

			return writer.ToString();
		}

		private static void Trace(string text, string id)
		{
			var date = DateTime.Now.ToString("HH:mm:ss|fff");
			id += new string(' ', Math.Max(20 - id.Length, 0));

			var output = $"{date}   {id} {text}";

			TraceLine(output);
		}

		private static void TraceLine(string output)
		{
			System.Diagnostics.Trace.WriteLine(output);
		}

		private static void WriteException(Exception e, TezosTraceWriter writer)
		{
			writer.WriteLine();
			writer.WritePaddedLine(e.GetType().Name, '-', 60);
			writer.WriteLine();

			if (e is ITraceException te)
			{
				writer.WriteLine(e.Message);
				writer.WriteLine();

				te.WriteTrace(writer);
			}
			else
			{
				writer.WriteLine(e.Message);
			}
		}
	}
}