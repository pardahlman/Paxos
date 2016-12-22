using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paxos.Strategies
{
	public interface IPropositionStrategy
	{
		Task<Proposition> CreateAsync(List<Consensus> latest, object value, CancellationToken token = default(CancellationToken));
		Task<Proposition> CreateAsync(List<Reject> rejects, CancellationToken token = default(CancellationToken));
	}
}