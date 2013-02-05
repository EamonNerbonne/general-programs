struct DnaSequence {
	const int BasesPerRawLog = 4;
	readonly uint[] rawdata;
	public readonly int Length;
	public DnaSequence(IEnumerable<Base> bases) {
		var list = new List<uint>(64);
		int i = 0;
		uint next = 0;
		foreach (var b in bases) {
			next |= (uint)b << i * 2;
			i++;
			if (i == 16) {
				i = 0;
				list.Add(next);
				next = 0;
			}
		}
		Length = list.Count * 16 + i;
		if (i != 0) list.Add(next);
		rawdata = list.ToArray();
	}

	public Base this[int i] { get { return (Base)(rawdata[i >> 4] >> (i & 15) * 2 & 3); } }
}
