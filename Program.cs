using System;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Использование: LogProcessor <inputFile> <outputFile> <problemsFile>");
            return;
        }

        var processor = new LogProcessor();
        processor.ProcessLogs(args[0], args[1], args[2]);
    }
}
