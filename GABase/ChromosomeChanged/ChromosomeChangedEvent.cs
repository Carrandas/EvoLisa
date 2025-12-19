using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GABase.ChromosomeChanged
{
	class ChromosomeChangedEvent
	{
		public Chromosome OldChromosome { get; set; }
		public Chromosome NewChromosome { get; set; }
	}
}
