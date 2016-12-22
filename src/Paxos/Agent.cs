using System.Collections.Concurrent;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace Paxos
{
	public class Agent
	{
		private readonly IAcceptor _acceptor;
		private readonly IWitness _witness;
		private readonly ILeader _leader;

		public Agent(IAcceptor acceptor, IWitness witness, ILeader leader)
		{
			_acceptor = acceptor;
			_witness = witness;
			_leader = leader;
		}
	}
}
