namespace Paxos
{
	public class Agree
	{
		public object Topic { get; set; }
		public object Value { get; set; }
		public int Id { get; set; }
	}

	public class Disagree
	{
		public object Topic { get; set; }
		public object Value { get; set; }
		public int Id { get; set; }
	}
}