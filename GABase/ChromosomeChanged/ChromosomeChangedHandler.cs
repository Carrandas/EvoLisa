using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GABase.ChromosomeChanged
{
	internal class ChromosomeChangedHandler : IObserver<ChromosomeChangedEvent>
	{
		public void OnNext(ChromosomeChangedEvent value)
		{
			throw new NotImplementedException();
		}

		public void OnError(Exception error)
		{
			throw new NotImplementedException();
		}

		public void OnCompleted()
		{
			throw new NotImplementedException();
		}
	}
}
