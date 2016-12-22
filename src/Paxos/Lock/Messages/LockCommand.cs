namespace Paxos.Lock.Messages
{
	public abstract class LockCommand
	{
		public object Resource { get; set; }
		public object Client { get; set; }
		public object User { get; set; }
	}
}