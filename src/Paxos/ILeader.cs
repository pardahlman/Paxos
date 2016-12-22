using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Paxos
{
	public interface ILeader
	{
		Task<List<Consensus>> GetLatestAsync(object topic, CancellationToken token = default(CancellationToken));
		Task<List<Vote>> ProposeAsync(Proposition proposition, CancellationToken token = default(CancellationToken));
		Task<List<Vote>> AnnounceAsync(Consensus consensus, CancellationToken token = default(CancellationToken));
	}

	public interface IAgent : IWitness, ILeader, IAcceptor
	{
		Guid Id { get; }
	}

	public interface ITransport
	{
		object Id { get; }
		Task PublishAsync<TMessage>(TMessage message, CancellationToken token = default(CancellationToken));
		Task SubscribeAsync<TMessage>(Func<TMessage, Task> handler, CancellationToken token = default(CancellationToken));
	}

	public abstract class Vote
	{
		public object Value { get; set; }
		public object Topic { get; set; }
		public int Id { get; set; }
	}

	public class Accept : Vote { }

	public class Reject : Vote
	{
		public int CurrentId { get; set; }
		public object CurrentValue { get; set; }
	}

	public class Proposition
	{
		public object Topic { get; set; }
		public int Id { get; set; }
		public object Value { get; set; }
	}

	public class Commitment : Proposition
	{
	}

	public class Consensus : Commitment
	{
	}

	public class CollectPrevious
	{
		public object Topic { get; set; }
	}

	public class PreviousConsensus : Consensus { }
}
