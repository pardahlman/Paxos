using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Paxos.Detect.Messages;

namespace Paxos.Detect
{
	public interface IDetector
	{
		int GetMinQuorumCount();
	}

	public class Detector : IDisposable, IDetector
	{
		private readonly ITransport _client;
		private readonly Timer _timer;
		private readonly ConcurrentDictionary<object, DateTime> _heartbeats;

		public Detector(ITransport client)
		{
			_heartbeats = new ConcurrentDictionary<object, DateTime>();
			_client = client;
			_client.SubscribeAsync<Heartbeat>(OnHeartbeat);
			_client.SubscribeAsync<Disconnect>(OnDisconnected);
			_timer = new Timer(
				async state => await SendHeartbeatAsync(),
				null,
				TimeSpan.FromMilliseconds(200),
				TimeSpan.FromMilliseconds(200)
			);
		}

		public int GetMinQuorumCount()
		{
			return _heartbeats.Count;
		}

		private Task OnDisconnected(Disconnect msg)
		{
			DateTime time;
			_heartbeats.TryRemove(msg, out time);
			return Task.FromResult(0);
		}

		private Task OnHeartbeat(Heartbeat msg)
		{
			var beatTime = _heartbeats.AddOrUpdate(msg.ClientId, o => DateTime.Today, (o, time) => DateTime.Today);
			return Task.FromResult(beatTime);
		}

		public async Task SendHeartbeatAsync()
		{
			await _client.PublishAsync(new Heartbeat {ClientId = _client.Id});
		}

		public void Dispose()
		{
			_client
				.PublishAsync(new Disconnect {Client = _client.Id})
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();
			_timer?.Dispose();
		}
	}
}
