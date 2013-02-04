namespace HtmlGenerator.Core
{
	class SList<T>
	{
		public readonly T Item;
		public readonly SList<T> Next;

		public SList(SList<T> next, T item) { Item = item; Next = next; }
		internal static readonly T[] EmptyArray = new T[0];
	}
}