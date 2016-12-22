using System.Threading.Tasks;

namespace Paxos.Strategies
{
	public interface IConsensusStrategy
	{
		Task<Consensus> CreateAsync(Proposition proposal);
	}
}