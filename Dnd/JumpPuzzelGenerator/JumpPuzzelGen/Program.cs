using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JumpPuzzelGen
{
	struct Pos:IEquatable<Pos> {
		public short x,y;
		public Pos(int x, int y):this((short)x,(short)y){}
		public Pos(short x,short y) {
			this.x=x;
			this.y=y;
		}
		public override int GetHashCode() {
			return ((((int)x) << 16) + ((int)y)).GetHashCode();
		}
		public override bool Equals(object obj) {
			return obj is Pos && this.Equals((Pos)obj);
		}
		public bool Equals(Pos other) {
			return other.x == x && other.y == y;
		}
		public static bool operator ==(Pos a, Pos b) {
			return a.Equals(b);
		}
		public static bool operator !=(Pos a, Pos b) {
			return !a.Equals(b);
		}
	}
	class PuzzelSolver
	{
		byte[,] puzzel;
		int[,] steps;
		Queue<Pos> toLook = new Queue<Pos>();
		Pos start, end;
		int width, height;
		public int fieldCount=0;
		public int phase;
		public int legalMoves;
		public int fieldSum;
		public PuzzelSolver(byte[,] puzzel,Pos start,int puzVal) {
			this.fieldSum = puzVal;
			this.puzzel = puzzel;
			//this.end = end;
			this.start = start;
			//puzzel[start.x, start.y] = (byte)1;

			width = puzzel.GetLength(0);
			height = puzzel.GetLength(1);
			steps = new int[width, height];
			toLook.Enqueue(start);
			steps[start.x, start.y] = 1;
		}

		public int Solve() {
			int movesMade=0;
			int legalMoves = 0;
			Pos next=default(Pos);
			while(toLook.Count > 0) {
				next = toLook.Dequeue();
				fieldCount++;
				movesMade = steps[next.x, next.y];
				//if(next == end) return movesMade;
				movesMade++;
				foreach(Pos newPos in CalcMoves(next)) {
					legalMoves++;
					if(steps[newPos.x, newPos.y] == 0) { //as yet unreached!
						steps[newPos.x, newPos.y] = movesMade;
						toLook.Enqueue(newPos);
					}
				}
			}
			end = next;
			phase = movesMade - 1;
			return phase;
		}

		public int Quality { get { return phase * phase + 15 * fieldCount - 10*end.x * (width - 1 - end.x) * end.y * (height - 1 - end.y) - 3*(end.x - start.x) * (end.x - start.x) - 3*(end.y - start.y) * (end.y - start.y) - 3 * fieldSum; } }
		public void Print() {
			Console.WriteLine("=============PUZZEL==============");
			for(int y = 0; y < height; y++) {
				for(int x = 0; x < width; x++) {
					int pos = (int)puzzel[x, y];
					if(x == start.x && y == start.y) Console.Write("["+pos+"]");
					else if(x== end.x && y==end.y)Console.Write("("+pos+")");
					else Console.Write(" "+pos+" ");
				}
				Console.WriteLine();
			}
			Console.WriteLine("=============Solution==============");
			for(int y = 0; y < height; y++) {
				for(int x = 0; x < width; x++) {
					int pos = (int)steps[x, y];
					string rep=Convert.ToString(pos,16).PadRight(3);
					Console.Write(rep);
				}
				Console.WriteLine();
			}
			Console.WriteLine("======Solution in:" + phase + "  Field Height:" + fieldSum + "  Reachable#:" + fieldCount + " Quality:"+Quality);
		}


		IEnumerable<Pos> CalcMoves(Pos at) {
			short val = (short)puzzel[at.x, at.y];
			if(val == 0) yield break;
			if(at.x + val < width) yield return new Pos(at.x + val, at.y);
			if(at.x - val >=0) yield return new Pos(at.x - val, at.y);
			if(at.y + val < height) yield return new Pos(at.x, at.y+val);
			if(at.y - val >= 0) yield return new Pos(at.x, at.y - val);
		}

	}

	class PuzzleCreator
	{
		static Random r = new Random();
		int width, height;
		int fieldCount;
		int[] nums;
		public byte[,] puzzel;
		int legalMoves;
		public Pos start;
		public int fieldSum;
		Pos[] todo;
		int todoPhaseStart;
		int todoTotalEnd;
		int ones;
		int todoPhaseEnd;
		Pos end;
		public int quality;
		public int phase;
		public PuzzleCreator(int width, int height){
			this.width = width;
			this.height = height;
			nums = new int[width + height];
			puzzel = new byte[width, height];
			todo = new Pos[width * height*4];
		}

		public void Print() {
			Console.WriteLine("=============PUZZEL==============");
			for(int y = 0; y < height; y++) {
				for(int x = 0; x < width; x++) {
					int pos = (int)puzzel[x, y];
					if(x == start.x && y == start.y) Console.Write("[" + pos + "]");
					else if(x == end.x && y == end.y) Console.Write("(" + pos + ")");
					else Console.Write(" " + pos + " ");
				}
				Console.WriteLine();
			}
			Console.WriteLine("==MinPath:" + phase + "  FieldSum:" + fieldSum + "  Reachable#:" + fieldCount +"  Possible Moves:"+legalMoves+ " Quality:" + quality);
		}


		int DetermineReachableNewNumber(Pos place) {
			int at=0;
			bool[] poss = new bool[10];
			for(int i = 0; i < place.x; i++)
				if(puzzel[i, place.y] == 0) {
					int step =place.x - i;
					if(!poss[step]) {
						poss[step]=true;
						nums[at++]=step;
					}
				}
			for(int i = place.x+1; i < width; i++)
				if(puzzel[i, place.y] == 0){
					int step =i-place.x;
					if(!poss[step]) {
						poss[step]=true;
						nums[at++]=step;
					}
				}
			for(int i = 0; i < place.y; i++)
				if(puzzel[place.x, i] == 0){
					int step =place.y - i;
					if(!poss[step]) {
						poss[step]=true;
						nums[at++]=step;
					}
				}
			for(int i = place.y + 1; i < height; i++)
				if(puzzel[place.x, i] == 0){
					int step =i-place.y;
					if(!poss[step]) {
						poss[step]=true;
						nums[at++]=step;
					}
				}
			if(at == 0) return 0;//no success!
			else return nums[r.Next(at)];
		}
		int DetermineMaxNumber(Pos place) {
			int step = Math.Min(9, Math.Max(Math.Max(place.x,width-1-place.x), Math.Max(place.y,height-1-place.y)));
			return (r.Next(step*3-2)+2)/3+1; //1 upto step
		}
		int DetermineRandomNumber(Pos place,ref bool phaseSafe) {
			return r.Next(1, width);
		}

		struct ValStepTuple
		{
			public int step;
			public int val;
		}
		IEnumerable<ValStepTuple> AccessibleField(Pos place) {
			for(int i = 0; i < place.x; i++) 
				yield return new ValStepTuple { val = puzzel[i, place.y], step = place.x - i };
			for(int i = place.x + 1; i < width; i++) 
				yield return new ValStepTuple { val = puzzel[i, place.y], step = i- place.x };
			for(int i = 0; i < place.y; i++) 
				yield return new ValStepTuple { val = puzzel[place.x, i], step = place.y - i };
			for(int i = place.y + 1; i < height; i++) 
				yield return new ValStepTuple { val = puzzel[place.x, i], step = i-place.y };
		}
		int DetermineWeightedNewNumber(Pos place,ref bool phaseSafe) {
			int zeros = 0;
			int[] vals = new int[10];
			vals[1] = 2;
			vals[2] = 1;
			foreach(var tuple in AccessibleField(place)) {
				int val = tuple.val;
				int step = tuple.step;
				if(val != 0) {
					vals[val]++;
					if(step == val) vals[step]++;
				} else if(!phaseSafe) {
					vals[step] -= 10000;
					zeros++;
				}
			}
			if(zeros > 0) phaseSafe = true;
			int minI = 1;
			int maxstep = Math.Min(9, Math.Max(Math.Max(place.x, width - 1 - place.x), Math.Max(place.y, height - 1 - place.y)));
			foreach(int i in PhaseSeq(1, maxstep+1, r.Next(1, maxstep+1))) {
				if(vals[i] <= vals[minI]) minI = i;
			}
			return minI;
		}
	   int CalcQualityWithEnd(Pos end){ 
			return 
				phase * width*height
				- 20 * end.x * (width - 1 - end.x) * end.y * (height - 1 - end.y) 
				- 5 * (end.x - start.x) * (end.x - start.x) 
				- 5 * (end.y - start.y) * (end.y - start.y) 
				+ 10 * fieldSum
				+ 75*(legalMoves-ones)
				- width*height*Math.Max(20-phase,0); 
		} 

		IEnumerable<Pos> CalcMoves(Pos from, short step) {
			if(from.x + step < width) yield return new Pos(from.x + step, from.y);
			if(from.x - step >= 0) yield return new Pos(from.x - step, from.y);
			if(from.y + step < height) yield return new Pos(from.x, from.y + step);
			if(from.y - step >= 0) yield return new Pos(from.x, from.y - step);
		}

		void RandomStart() {
			int startPosInd = r.Next(2 * (width + height) - 4);
			if(startPosInd < width) start = new Pos(startPosInd, 0);
			else if(startPosInd - width < width) start = new Pos(startPosInd - width, height - 1);
			else if(startPosInd - 2 * width < height - 2) start = new Pos(0, startPosInd - 2 * width + 1);
			else start = new Pos(width - 1, startPosInd - 2 * width - height + 3);
		}


		private static IEnumerable<int> PhaseSeq(int phaseStart, int phaseEnd,int phaseCut) {
			for(int i = phaseCut; i < phaseEnd; i++)
				yield return i;
			for(int i = phaseStart; i < phaseCut; i++)
				yield return i;
		}

		public void CreateByPath() {
			RandomStart();
			Array.Clear(puzzel, 0, puzzel.Length);// puzzel.Initialize();
			fieldSum = 0;
			fieldCount = 0;
			quality = Int32.MinValue;
			phase = 0;
			ones = 0;
			todoTotalEnd = 0;
			legalMoves = 0;
			todoPhaseStart = 0;
			todo[todoTotalEnd++] = start;
			todoPhaseEnd = todoTotalEnd;
			while(todoPhaseStart != todoTotalEnd) {
				bool posterityEnsured=false;
				foreach(int posI in PhaseSeq(todoPhaseStart,todoPhaseEnd,r.Next(todoPhaseStart,todoPhaseEnd))) {
					Pos next = todo[posI];
					if(puzzel[next.x, next.y] != 0) continue;//you might get the same element twice in a phase if reachable multiple times from the previous phase.
					int val = 0;
					val = DetermineRandomNumber(next, ref posterityEnsured);
							//DetermineWeightedNewNumber(next,ref  posterityEnsured);
					puzzel[next.x,next.y] = (byte)val;
					fieldSum+=val;
					if(val == 1) {
						ones++;
						foreach(Pos newPos in CalcMoves(next, (short)1)) 
							if(puzzel[newPos.x, newPos.y] == 1) //adjacent ones
								ones++;//extra bad!
					}
					fieldCount++;
					foreach(Pos newPos in CalcMoves(next, (short)val)) {
						legalMoves++;
						if(puzzel[newPos.x, newPos.y] == 0) {//meaning next phase you'll only get new things
							todo[todoTotalEnd++] = newPos;
							posterityEnsured = true;
						}
					}
				}
				phase++;//finished one level of recursive descent - dig deeper!
				if(!posterityEnsured) break; //no point in continuing and needlessly mucking with the "phase" variable.
				else {
					todoPhaseStart = todoPhaseEnd;
					todoPhaseEnd = todoTotalEnd;
				}
			}
			for(int i = todoPhaseStart; i < todoPhaseEnd; i++) {
				int newqual = CalcQualityWithEnd(todo[i]);
				if(newqual > quality) {
					end = todo[i];
					quality = newqual;
				} 
			}
		}
	}

	class Program
	{
		static void Main(string[] args) {
			Random r = new Random();
			int width = 5,height=5;
			PuzzleCreator creator = new PuzzleCreator(width, height);
			//int bestSolLength = 0;
			//int bestPuzVal = 0;
			//int bestStepCount = 0;
			int bestQual = Int32.MinValue;
			int prevBestQual = Int32.MinValue;
			int tries = 0;
			DateTime startTime = DateTime.Now;
			DateTime lastPrint = DateTime.Now;
			long totSol = 0;
			long totQ = 0;
			while(!Console.KeyAvailable) {
				creator.CreateByPath();

				int solutionQ = creator.quality;
				totSol += creator.phase;
				totQ += solutionQ;
				if(solutionQ >bestQual) {
						creator.Print();
						prevBestQual = bestQual;
						bestQual = solutionQ;
					Console.WriteLine("--------------------Best yet!");
				} else if(solutionQ > prevBestQual) {
					creator.Print();
				}
				tries++;
				if((DateTime.Now - lastPrint).TotalSeconds > 10.0) {
					lastPrint=DateTime.Now;
					Console.WriteLine("Average nps: {0:f2}, Average solution: {1:f4}, AvQ: {2:f1}", tries / (lastPrint - startTime).TotalSeconds,(double)totSol/(double)tries,(double)totQ/(double)tries);
				}
			}
		}
	}
}
