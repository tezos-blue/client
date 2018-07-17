using System.Collections.Generic;

namespace SLD.Tezos.Client
{
	public class ModelCollection<T> : List<T>
	{
		public ModelCollection()
		{
		}

		public ModelCollection(IList<T> collection) : base(collection)
		{
		}
	}
}