namespace HtmlGenerator
{
	public class BuildHFragment : ChildFactory<BuildHFragment>
	{
		readonly SList<HNode> children;
		BuildHFragment(SList<HNode> children) { this.children = children; }
		public BuildHFragment() { }

		public override BuildHFragment this[HNode node] { get { return new BuildHFragment(children.Prepend(node)); } }

		public HFragment End { get { return new HFragment(children.ToArray()); } }
	}
}