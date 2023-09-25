using Sharpener;

return Main(args);

int Main(string[] args)
{
    if (args.Length != 2)
    {
        var exeFilename = Path.GetFileName(Environment.ProcessPath);
        Console.Error.WriteLine($"Usage: {exeFilename} binaries-dir trace - symbolicate a decorated stack trace using dlls and pdbs from binaries-dir");
        return 1;
    }

    var binariesPath = args[0];
    var tracePath = args[1];

    if (!Directory.Exists(binariesPath))
    {
        Console.Error.WriteLine($"Binaries path '{binariesPath}' does not exist");
        return 2;
    }

    if (!File.Exists(tracePath))
    {
        Console.Error.WriteLine($"Stack trace not found at '{tracePath}'");
        return 2;
    }

    var lines = File.ReadAllLines(tracePath);

    Symbolicator.Symbolicate(binariesPath, lines);

    return 0;
}
