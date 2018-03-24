using System;
using System.Diagnostics;

namespace SLD.Tezos
{
    public class TezosObject
    {
		public void Trace(string text)
        {
            Tracer.Trace(this, text);
        }

        public void Trace(Exception e)
        {
            Tracer.Trace(this, e);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}