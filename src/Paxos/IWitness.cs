using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Paxos
{
	public interface IWitness
	{
		Task LearnAsync(Consensus consensus);
		Task<Consensus> GetLatestAsync();
	}

	internal class Witness : IWitness
	{
		private readonly ConcurrentDictionary<object, ConcurrentBag<Consensus>> _topicConsensus;

		public Witness()
		{
			_topicConsensus = new ConcurrentDictionary<object, ConcurrentBag<Consensus>>();
		}

		public Task LearnAsync(Consensus consensus)
		{
			_topicConsensus.AddOrUpdate(consensus.Topic, o => new ConcurrentBag<Consensus> {consensus}, (o, bag) =>
			{
				bag.Add(consensus);
				return bag;
			});
			return Task.FromResult(0);
		}

		public Task<Consensus> GetLatestAsync()
		{
			throw new System.NotImplementedException();
		}
	}
}