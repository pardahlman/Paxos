using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Paxos.Lock.Messages;
using Paxos.Strategies;

namespace Paxos.Lock
{
	public class LockManager
	{
		private readonly IAgent _agent;
		private readonly IPropositionStrategy _propositionStrategy;
		private readonly IRetryStrategy _retryStrategy;
		private readonly IConsensusStrategy _consensusStrategy;

		public LockManager(IAgent agent, IPropositionStrategy propositionStrategy, IRetryStrategy retryStrategy, IConsensusStrategy consensusStrategy)
		{
			_agent = agent;
			_propositionStrategy = propositionStrategy;
			_retryStrategy = retryStrategy;
			_consensusStrategy = consensusStrategy;
		}

		public Task RequestAsync(object resource, object user, CancellationToken token = default(CancellationToken))
		{
			return PerformAsync(new RequestLock
			{
				Client = _agent.Id,
				User = user,
				Resource = resource,
			}, token);
		}

		public Task ReleaseAsync(object resource, object user, CancellationToken token = default(CancellationToken))
		{
			return PerformAsync(new ReleaseLock
			{
				Client = _agent.Id,
				Resource = resource,
				User = user
			}, token);
		}

		public async Task SubscribeAsync(object resource, Func<object, Task> callback)
		{
		}

		protected virtual async Task PerformAsync(LockCommand command, CancellationToken token = default(CancellationToken))
		{
			var latest = await _agent.GetLatestAsync(command, token);
			var proposal = await _propositionStrategy.CreateAsync(latest, command, token);

			_retryStrategy.InvokeAsync(async () =>
			{
				var votes = await _agent.ProposeAsync(proposal, token);
				var rejects = votes.OfType<Reject>().ToList();
				var accept = votes.OfType<Accept>().ToList();
				var success = accept.Count > rejects.Count;
				if (!success)
				{
					proposal = await _propositionStrategy.CreateAsync(rejects, token);
				}
				return success;
			});

			var consensus = await _consensusStrategy.CreateAsync(proposal);
			_retryStrategy.InvokeAsync(async () =>
			{
				var votes = await _agent.AnnounceAsync(consensus, token);
				var rejects = votes.OfType<Reject>().ToList();
				var accept = votes.OfType<Accept>().ToList();
				var success = accept.Count > rejects.Count;
				if (!success)
				{
					proposal = await _propositionStrategy.CreateAsync(rejects, token);
				}
				return success;
			});
		}
	}
}
