using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Linq;
using EamonExtensionsLinq;
using System.IO;

namespace SongDataLib
{
	public static class SongUtil
	{
/// <summary>
/// Converts a "normalized" song to a byte array using only numbers 0-56 normally, and 57 as error value.
/// </summary>
/// <param name="str"></param>
/// <returns></returns>
		public static byte[] str2byteArr(string str) {
			List<byte> bytArr = new List<byte>(str.Length);
			foreach(char c in str) {
				int b = (ushort)c;
				if(b >= 97 && b < 123) bytArr.Add((byte)(b - 68));//29-54			==11115598 - letters
				else if(b >= 37 && b < 60) bytArr.Add((byte)(b - 34));//3- 25		==3191357 -numbers, symbols
				else if(b == 32 || b == 33) bytArr.Add((byte)(b - 32));//0-1		==1667004 - space, !
				else if(b == 63 || b == 64) bytArr.Add((byte)(b - 36));//27-28	==9586 - ?, @
				else if(b == 35) bytArr.Add(2);//2										==4073 - #
				else if(b == 61) bytArr.Add(26);//26									==1380 - '='
				else if(b == 126) bytArr.Add(56);//55									==561 - '~'
				else if(b == 124) bytArr.Add(55);//55									==544 - '|'
				else bytArr.Add(57);//should never happen!						==0
			}
			return bytArr.ToArray();
		}

		public const byte MAXCANONBYTE = 57;

		public static byte[] CanonicalizedSearchStr(string str) {
			return str2byteArr(EamonExtensionsLinq.Text.Canonicalize.Basic(str));
		}

		public static bool Contains(byte[] elem, byte[] substring) {
			for(int i = 0; i <= elem.Length - substring.Length; i++) {
				bool match = true;
				for(int j = 0; j < substring.Length; j++) {
					if(elem[i + j] != substring[j]) {
						match = false;
						break;
					}
				}
				if(match)
					return true;
			}
			return false;
		}


		public static IEnumerable<int> ZipUnion(IEnumerable<int> a, IEnumerable<int> b) {
			var enumA = a.GetEnumerator();
			var enumB = b.GetEnumerator();
			int elA, elB;
			if(enumA.MoveNext()) {
				if(enumB.MoveNext()) {
					elA = enumA.Current;
					elB = enumB.Current;
					while(true) {
						if(elA < elB) {
							yield return elA;
							if(enumA.MoveNext()) elA = enumA.Current;
							else {//no more a's
								yield return elB;
								while(enumB.MoveNext()) yield return enumB.Current;
								break;
							}
						} else {
							yield return elB;
							if(enumB.MoveNext()) elB = enumB.Current;
							else {//no more b's!
								yield return elA;
								while(enumA.MoveNext()) yield return enumA.Current;
								break;
							}
						}
					}
				} else {
					yield return enumA.Current;
					while(enumA.MoveNext()) yield return enumA.Current;
				}
			} else while(enumB.MoveNext()) yield return enumB.Current;
		}

		public static IEnumerable<int> ZipIntersect(IEnumerable<int> a, IEnumerable<int> b) {
			var enumA = a.GetEnumerator();
			var enumB = b.GetEnumerator();

			if(!enumA.MoveNext() || !enumB.MoveNext()) yield break;
			int elA = enumA.Current;
			int elB = enumB.Current;
			while(true) {
				if(elA == elB) {
					yield return elA;
					while(elA == elB && enumB.MoveNext()) elB = enumB.Current;
					if(elA == elB) yield break;
					if(!enumA.MoveNext()) yield break;
				} else if(elA < elB) {
					if(!enumA.MoveNext()) yield break;
					elA = enumA.Current;
				} else {
					if(!enumB.MoveNext()) yield break;
					elB = enumB.Current;
				}
			}
		}
		
		public static IEnumerable<int> RemoveDup(IEnumerable<int> orderedList) {
			var orderedEnum = orderedList.GetEnumerator();
			if(!orderedEnum.MoveNext()) yield break;
			int current = orderedEnum.Current;
			yield return current;
			while(orderedEnum.MoveNext()) {
				int newVal = orderedEnum.Current;
				if(newVal != current) {
					current = newVal;
					yield return current;
				}
			}
		}
	}
}
/* In song distributions now:
32 ' ' 1661752
33 '!' 5252
34 '"' 0
35 '#' 4073
36 '$' 0
37 '%' 802
38 '&' 17529
39 ''' 37428
40 '(' 74295
41 ')' 72898
42 '*' 971
43 '+' 2646
44 ',' 21701
45 '-' 424638
46 '.' 191197
47 '/' 442778
48 '0' 571773
49 '1' 264906
50 '2' 229435
51 '3' 169876
52 '4' 70144
53 '5' 103525
54 '6' 79437
55 '7' 60354
56 '8' 66092
57 '9' 135908
58 ':' 113888
59 ';' 2136
60 '<' 0
61 '=' 1380
62 '>' 0
63 '?' 1457
64 '@' 8129
 *
97 'a' 839165
98 'b' 275422
99 'c' 502126
100 'd' 486664
101 'e' 1081085
102 'f' 170384
103 'g' 254648
104 'h' 367725
105 'i' 897146
106 'j' 57296
107 'k' 165142
108 'l' 578790
109 'm' 529835
110 'n' 656362
111 'o' 827122
112 'p' 337183
113 'q' 10720
114 'r' 671772
115 's' 742937
116 't' 691267
117 'u' 409526
118 'v' 122634
119 'w' 124188
120 'x' 47765
121 'y' 190360
122 'z' 78334
123 '{' 0
124 '|' 544
125 '}' 0
126 '~' 561
 * 
 * i.e. 62 entries, of which 58 non-zero.
*/