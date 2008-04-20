using System;
class Program
{
    static void Main()
    {
        object[] array = { "hello", "world", "!" };
        array[2] = 1;
        array = new []{ "hello", "world", "!" };
        array[2] = 1;
        Console.WriteLine(array[2].ToString());
    }
}