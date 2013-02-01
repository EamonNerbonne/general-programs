namespace HtmlGenerator.Base
{
	public interface INodeBuilder<out TSelf> where TSelf : INodeBuilder<TSelf>
	{
		TSelf this[HNode node] { get; }
	}
}