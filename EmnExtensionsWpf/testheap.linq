<Query Kind="Statements">
  <Reference Relative="..\EmnExtensions\bin\Release\EmnExtensions.dll">D:\EamonLargeDocs\VersionControlled\docs-trunk\programs\EmnExtensions\bin\Release\EmnExtensions.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationCore.dll</Reference>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Xml.Linq</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>EmnExtensions</Namespace>
  <Namespace>EmnExtensions.Algorithms</Namespace>
  <Namespace>EmnExtensions.DebugTools</Namespace>
  <Namespace>System.Windows.Media</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>EmnExtensions.MathHelpers</Namespace>
  <Namespace>EmnExtensions.Collections</Namespace>
</Query>

var rng = new MersenneTwister(13);
int[] rndNums = Enumerable.Range(0,5000000).Select(i=>rng.Next()).ToArray();

Action<string, IHeap<int>> test = (name,heap) => {
	long sum=0;
	using(new DTimer(name)) {
		foreach(int val in rndNums)		{
			sum+=val;
			if(heap.Count>0) {
				if(heap.Count > (val &0xfffff)) {
					while(heap.Count>0)
						heap.RemoveTop();
				} else if((heap.Count&0xff) == (val &0xff))
					heap.RemoveTop();
			}
			heap.Add(val&0xfff);
		}
		heap.ElementsInRoughOrder.Sum().Dump();
		while(heap.Count>0)
			heap.RemoveTop();
	
	}
	sum.Dump();
};
test("noindex", Heap.Create<int,Heap.FastIntComparer>(new Heap.FastIntComparer()));
test("index", Heap.CreateIndexable<int>(null));