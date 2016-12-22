using System;
using System.Threading.Tasks;

namespace Paxos.Strategies
{
	public interface IRetryStrategy
	{
		void InvokeAsync(Func<Task<bool>> func);
	}
}