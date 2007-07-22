using System;

class ArrayTest {
    static int Main(string[] args) {
        TimeSpan jagInit, jagTime, sqInit, sqTime;
        jagInit = jagTime = sqTime = sqInit = TimeSpan.Zero;
        DateTime laststart;
        int iters = Int32.Parse(args[0]);
        int size = Int32.Parse(args[1]);
        int benchmark = 0;
        for (int iterIndex = 0; iterIndex < iters; iterIndex++) {
            laststart = DateTime.Now;
            int[][] jagged = new int[size][];
            for (int i = 0; i < size; i++)
                jagged[i] = new int[size];
            jagInit += DateTime.Now - laststart;
            laststart = DateTime.Now;
            int[,] square = new int[size, size];
            sqInit += DateTime.Now - laststart;
            laststart = DateTime.Now;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++) {
                    jagged[i][j] = i * size + j;
                }
            jagTime += DateTime.Now - laststart;
            laststart = DateTime.Now;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++) {
                    square[i, j] = i * size + j;
                }
            sqTime += DateTime.Now - laststart;
            laststart = DateTime.Now;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++) {
                    jagged[i][j] += jagged[j][i];
                }
            jagTime += DateTime.Now - laststart;
            laststart = DateTime.Now;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++) {
                    square[i, j] = square[j, i];
                }
            sqTime += DateTime.Now - laststart;
            laststart = DateTime.Now;
            long total = 0;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++) {
                    total += jagged[i][j];
                }
            benchmark += (int)total % 2;
            jagTime += DateTime.Now - laststart;
            laststart = DateTime.Now;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++) {
                    total += square[i, j];
                }
            benchmark += (int)total % 2;
            sqTime += DateTime.Now - laststart;
            laststart = DateTime.Now;
        }
        Console.WriteLine("\njagInit: " + jagInit + "\njagRun:" + jagTime + "\nsqInit:" + sqInit + "\nsqRun:" + sqTime);
        return benchmark;
    }
}
