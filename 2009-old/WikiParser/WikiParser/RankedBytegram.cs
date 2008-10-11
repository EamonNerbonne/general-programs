using System.Linq;
using System.Text;

namespace WikiParser
{
    /// <summary>
    /// This struct implements 5-gram of bytes, which are stored in an 8-byte 64-bit integer.  
    /// Crucially, this allows for simple integer comparisons to provide ordering, as opposed to using
    /// string comparisons, which improves LM-statistic calculation performance.
    /// The transformation of the byte-string to integers is order preserving, which is not actually important
    /// in this program (any deterministic ordering is fine), but allows for easy comparison with the 
    /// string-based ngram implementation: all data structures should be almost identical, including the
    /// ordering used in the LM-statistics comparison function.  Thatmakes debugging easier.
    /// 
    /// The structure could easily be extended up to an 8-gram, but extension beyond that (or if unicode support
    /// is relevant) can't be done losslessly.
    /// 
    /// Since words are converted to these n-grams, the CreateFromByteString implementation (which is performance-
    /// critical) tries to generate all n-grams with low overhead.
    /// </summary>
    public struct RankedBytegram
    {
        public RankedBytegram(RankedNgram stringbased) {
            this.ngramK = CreateFromBytes(Encoding.Default.GetBytes(stringbased.ngram));
            this.rank = stringbased.rank;
        }

        public ulong ngramK;
        public int rank;
        const int BITOFFSET = 4 * 8;
        public static ulong CreateFromBytes(byte[] arr) {
            return CreateFromBytes(arr, 0, arr.Length);
        }
        public static ulong CreateFromBytes(byte[] arr, int start, int end) {
            ulong retval = 0;
            int offset = BITOFFSET;
            while (start != end) {
                retval |= (ulong)arr[start] << offset;
                start++; offset -= 8;
            }
            return retval;
        }
        public static int CalcNgramCountFromLength(int len) {
            return
                len == 0 ? 0 :
                len == 1 ? 1 :
                len == 2 ? 3 :
                len == 3 ? 6 :
                len == 4 ? 10 :
                len * 5 - 10;
        }
        public static ulong[] CreateFromByteString(byte[] arr, ulong[] output, ref int writePtr) {
            return CreateFromByteString(arr, 0, arr.Length,output, ref writePtr);
        }
        public static ulong[] CreateFromByteString(byte[] arr, int start, int end,ulong[]output,ref int writePtr) {
            int len = end - start;
            //0,1,3,6..10..15..20.....
            int count = CalcNgramCountFromLength(len);

            ulong b;

            if (start == end) return output;
            b = ((ulong)arr[start]);// <<BITOFFSET;
            output[writePtr] = b << BITOFFSET; writePtr++;
            start++;

            if (start == end) return output;
            b = ((ulong)arr[start]); //<< BITOFFSET;
            output[writePtr] = b << BITOFFSET; writePtr++;
            output[writePtr] = output[writePtr - 2] | (b << (BITOFFSET - 8)); writePtr++;
            start++;

            if (start == end) return output;
            b = ((ulong)arr[start]);// << BITOFFSET;
            output[writePtr] = b << BITOFFSET; writePtr++;
            output[writePtr] = output[writePtr - 3] | (b << (BITOFFSET - 8)); writePtr++;
            output[writePtr] = output[writePtr - 3] | (b << (BITOFFSET - 16)); writePtr++;
            start++;

            if (start == end) return output;
            b = ((ulong)arr[start]);// << BITOFFSET;
            output[writePtr] = b << BITOFFSET; writePtr++;
            output[writePtr] = output[writePtr - 4] | (b << (BITOFFSET - 8)); writePtr++;
            output[writePtr] = output[writePtr - 4] | (b << (BITOFFSET - 16)); writePtr++;
            output[writePtr] = output[writePtr - 4] | (b << (BITOFFSET - 24)); writePtr++;
            start++;

            if (start == end) return output;
            b = ((ulong)arr[start]);// << BITOFFSET;
            output[writePtr] = b << BITOFFSET; writePtr++;
            output[writePtr] = output[writePtr - 5] | (b << (BITOFFSET - 8)); writePtr++;
            output[writePtr] = output[writePtr - 5] | (b << (BITOFFSET - 16)); writePtr++;
            output[writePtr] = output[writePtr - 5] | (b << (BITOFFSET - 24)); writePtr++;
            output[writePtr] = output[writePtr - 5] | b; writePtr++;
            start++;

            while (start < end) {
                b = ((ulong)arr[start]);// << BITOFFSET;
                output[writePtr] = b << BITOFFSET; writePtr++;
                output[writePtr] = output[writePtr - 6] | (b << (BITOFFSET - 8)); writePtr++;
                output[writePtr] = output[writePtr - 6] | (b << (BITOFFSET - 16)); writePtr++;
                output[writePtr] = output[writePtr - 6] | (b << (BITOFFSET - 24)); writePtr++;
                output[writePtr] = output[writePtr - 6] | b; writePtr++;
                start++;
            }

            return output;
        }
        public override string ToString() {
            return
                Encoding.Default.GetString(new[] {
                    (byte)(ngramK >> 32), 
                    (byte)(ngramK >> 24),
                    (byte)((ngramK >> 16) & 0xff), 
                    (byte)((ngramK >> 8) & 0xff), 
                    (byte)((ngramK) & 0xff),
                }.Where(b => b != 0).ToArray())
                  + ": " + rank;
        }
    }
}
