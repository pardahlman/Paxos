using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Paxos
{
	public interface IAcceptor
	{
		Task VoteAsync(Proposition proposition);
		Task AcceptAsync(Consensus consensus);
	}
	
	public class Acceptor : IAcceptor
	{
		private readonly ITransport _transport;
		private readonly ConcurrentDictionary<object, Vote> _topicVotes;
		public Acceptor(ITransport transport)
		{
			_transport = transport;
			_topicVotes = new ConcurrentDictionary<object, Vote>();
		}

		public Task VoteAsync(Proposition proposition)
		{
			var vote = _topicVotes.GetOrAdd(proposition.Topic,
				o => new Accept {Id = proposition.Id, Topic = proposition.Topic, Value = proposition.Value});
			if (proposition.Id < vote.Id || proposition.Value != vote.Value)
			{
				return _transport.PublishAsync(new Reject
				{
					Topic = proposition.Topic,
					Id = proposition.Id,
					Value = proposition.Value,
					CurrentId = vote.Id,
					CurrentValue = vote.Value
				});
			}
			return _transport.PublishAsync(new Accept
			{
				Topic = proposition.Topic,
				Id = proposition.Id,
				Value = proposition.Value
			});
		}

		public Task AcceptAsync(Consensus consensus)
		{
			Vote vote;
			if (!_topicVotes.TryGetValue(consensus.Topic, out vote))
			{
				return Task.FromResult(0);
			}
			if (vote.Id < consensus.Id && vote.Value == consensus.Topic)
			{
				return _transport.PublishAsync(new Agree
				{
					Topic = consensus.Topic,
					Value = consensus.Value,
					Id = consensus.Id
				});
			}
			return _transport.PublishAsync(new Disagree
			{
				Topic = consensus.Topic,
				Value = consensus.Value,
				Id = consensus.Id
			});
		}
	}
}