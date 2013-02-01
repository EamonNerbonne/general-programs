using HtmlGenerator.Core;

namespace HtmlGenerator.Exp1
{
	public struct HFragmentCompleter : IBuilderContext<HFragment, HFragment>
	{
		public HFragment Complete(HFragment node) { return node; }
	}

	public class BuildHFragment : ChildElemFactory<BuildHFragment, HFragment, HFragmentCompleter, HFragment>
	{
		readonly SList<HNode> children;
		BuildHFragment(SList<HNode> children) : base(new HFragmentCompleter())  { this.children = children; }
		public BuildHFragment() : base(new HFragmentCompleter()) { }

		internal override HFragment Finish() { return new HFragment(children.ToArray()); }

		public override BuildHFragment this[HNode node] { get { return new BuildHFragment(children.Prepend(node)); } }

	}
}