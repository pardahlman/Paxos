using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Paxos.Detect;

namespace Paxos
{
	public class Leader : ILeader
	{
		private readonly ITransport _client;
		private readonly IDetector _detector;
		private readonly TimeSpan _timeout;
		private readonly ConcurrentDictionary<object, SemaphoreSlim> _topicSemaphores;
		private readonly ConcurrentDictionary<object, ConcurrentBag<Consensus>> _topicConsensus;
		private readonly ConcurrentDictionary<object, ConcurrentBag<Vote>> _topicVotes;

		public Leader(ITransport client, IDetector detector)
		{
			_client = client;
			_detector = detector;
			_timeout = TimeSpan.FromMilliseconds(20);
			_topicSemaphores = new ConcurrentDictionary<object, SemaphoreSlim>();
			_topicConsensus = new ConcurrentDictionary<object, ConcurrentBag<Consensus>>();
			_topicVotes = new ConcurrentDictionary<object, ConcurrentBag<Vote>>();

			client.SubscribeAsync<Vote>(vote =>
			{
				_topicVotes.AddOrUpdate(vote.Topic,
					o => new ConcurrentBag<Vote> { vote },
					(o, bag) =>
					{
						bag.Add(vote);
						return bag;
					}
				);
				return Task.FromResult(0);
			});

			client.SubscribeAsync<Consensus>(consensus =>
			{
				_topicConsensus.AddOrUpdate(consensus.Topic,
					o => new ConcurrentBag<Consensus> { consensus },
					(o, bag) =>
					{
						bag.Add(consensus);
						return bag;
					}
				);
				return Task.FromResult(0);
			});
		}

		public Task<List<Consensus>> GetLatestAsync(object topic, CancellationToken token = default(CancellationToken))
		{
			var semaphore = _topicSemaphores.GetOrAdd(topic, o => new SemaphoreSlim(1, 1));
			var consensusTsc = new TaskCompletionSource<List<Consensus>>();
			semaphore
				.WaitAsync(token)
				.ContinueWith(async t =>
				{
					if (t.IsCanceled)
					{
						return;
					}
					_topicConsensus.AddOrUpdate(topic, o => new ConcurrentBag<Consensus>(), (o, bag) => new ConcurrentBag<Consensus>());
					await _client.PublishAsync(new PreviousConsensus(), token);
					Timer timer = null;
					timer = new Timer(state =>
					{
						timer?.Dispose();
						ConcurrentBag<Consensus> consensus;
						if (!_topicConsensus.TryGetValue(topic, out consensus))
						{
							consensusTsc.TrySetException(new Exception("Consensus not found"));
							return;
						}
						if (!consensus.Any())
						{
							consensusTsc.TrySetException(new Exception("No consensus recieved"));
							return;
						}
						consensusTsc.TrySetResult(consensus.ToList());
						semaphore.Release();
					}, null, _timeout, new TimeSpan(-1));
				}, token);
			return consensusTsc.Task;
		}

		public Task<List<Vote>> ProposeAsync(Proposition proposition, CancellationToken token = new CancellationToken())
		{
			var semaphore = _topicSemaphores.GetOrAdd(proposition.Topic, o => new SemaphoreSlim(1, 1));
			var votesTsc = new TaskCompletionSource<List<Vote>>();
			semaphore
				.WaitAsync(token)
				.ContinueWith(async t =>
				{
					if (t.IsCanceled)
					{
						return;
					}
					_topicConsensus.AddOrUpdate(proposition.Topic, o => new ConcurrentBag<Consensus>(), (o, list) => new ConcurrentBag<Consensus>());
					await _client.PublishAsync(proposition, token);
					Timer timer = null;
					timer = new Timer(state =>
					{
						timer?.Dispose();
						ConcurrentBag<Vote> votes;
						if (!_topicVotes.TryGetValue(proposition.Topic, out votes))
						{
							votesTsc.TrySetException(new Exception("Votes not found"));
							return;
						}
						var minCount = _detector.GetMinQuorumCount();

						if (votes.Count < minCount)
						{
							votesTsc.TrySetException(new Exception($"Not enough votes. Required {minCount}, but got {votes.Count}"));
							return;
						}
						votesTsc.TrySetResult(votes.ToList());
						semaphore.Release();
					}, null, _timeout, new TimeSpan(-1));
				}, token);
			return votesTsc.Task;
		}

		public Task<List<Vote>> AnnounceAsync(Consensus consensus, CancellationToken token = new CancellationToken())
		{
			var semaphore = _topicSemaphores.GetOrAdd(consensus.Topic, o => new SemaphoreSlim(1, 1));
			var votesTsc = new TaskCompletionSource<List<Vote>>();
			semaphore
				.WaitAsync(token)
				.ContinueWith(async t =>
				{
					if (t.IsCanceled)
					{
						return;
					}
					_topicVotes.AddOrUpdate(consensus.Topic, o => new ConcurrentBag<Vote>(), (o, list) => new ConcurrentBag<Vote>());
					await _client.PublishAsync(consensus, token);
					Timer timer = null;
					timer = new Timer(state =>
					{
						timer?.Dispose();
						ConcurrentBag<Vote> votes;
						if (!_topicVotes.TryGetValue(consensus.Topic, out votes))
						{
							votesTsc.TrySetException(new Exception("Votes not found"));
							return;
						}
						var minCount = _detector.GetMinQuorumCount();

						if (votes.Count < minCount)
						{
							votesTsc.TrySetException(new Exception($"Not enough votes. Required {minCount}, but got {votes.Count}"));
							return;
						}
						votesTsc.TrySetResult(votes.ToList());
						semaphore.Release();
					}, null, _timeout, new TimeSpan(-1));
				}, token);
			return votesTsc.Task;
		}
	}
}