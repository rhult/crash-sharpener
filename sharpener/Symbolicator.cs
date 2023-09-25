using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Sharpener;

public static class Symbolicator
{
    // Tries to symbolicate each line in the stack trace, leaving lines
    // unmodified if there is a problem.
    public static void Symbolicate(string binariesPath, string[] lines)
    {
        foreach (var line in lines)
        {
            var frame = StackFrame.FromString(line);
            if (!frame.HasValue)
            {
                PrintLine(line);
                continue;
            }

            var dllPath = FindDllPath(binariesPath, frame.Value.Symbol);
            if (dllPath == null)
            {
                PrintLine(line);
                continue;
            }

            var reader = GetMetadataReader(dllPath);
            if (reader == null)
            {
                PrintLine(line);
                continue;
            }

            try
            {
                PrintLine(SymbolicateFrame(frame.Value, reader));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Could not symbolicate line: {e}");
                PrintLine(line);
            }
        }
    }

    private static void PrintLine(string line)
    {
        Console.WriteLine(line);
    }

    private static string SymbolicateFrame(StackFrame frame, MetadataReader pdbReader)
    {
        var methodDebugInformationHandle = MetadataTokens.MethodDebugInformationHandle(frame.TokenValue);
        if (methodDebugInformationHandle.IsNil)
        {
            return frame.PartialLine;
        }

        var methodDebugInformation = pdbReader.GetMethodDebugInformation(methodDebugInformationHandle);

        // Find the source file.
        var documentHandle = methodDebugInformation.Document;
        string documentName;
        if (!documentHandle.IsNil)
        {
            var document = pdbReader.GetDocument(documentHandle);
            documentName = pdbReader.GetString(document.Name);
        }
        else
        {
            documentName = "?";
        }

        // Find the line number.
        var sequencePoints = methodDebugInformation.GetSequencePoints();
        if (!sequencePoints.Any())
        {
            return frame.PartialLine;
        }

        SequencePoint previousPoint = default;
        var hasPreviousPoint = false;
        foreach (var point in sequencePoints)
        {
            if (point.IsHidden)
            {
                continue;
            }

            if (point.Offset == frame.Offset - 0)
            {
                // This assumes the exception happened in one line. Output could be
                // improved to show start line + start column to end line + end
                // column, but testing shows that in most cases just the start line
                // is enough.
                return $"{frame.PartialLine} in {documentName}:{GetSourceLocation(point)}";
            }

            if (hasPreviousPoint && point.Offset > frame.Offset - 0)
            {
                break;
            }

            previousPoint = point;
            hasPreviousPoint = true;
        }

        if (hasPreviousPoint)
        {
            return $"{frame.PartialLine} in {documentName}:{GetSourceLocation(previousPoint)}";
        }

        return frame.PartialLine;
    }

    private static string GetSourceLocation(SequencePoint point)
    {
        return $"{point.StartLine} [{point.StartLine}:{point.StartColumn}-{point.EndLine}:{point.EndColumn}]";
    }

    private static string? RemoveSymbolTail(string symbol)
    {
        var index = symbol.LastIndexOf(".", StringComparison.InvariantCulture);
        return index == -1 ? null : symbol[..index];
    }

    // Best-effort mapping of symbol to dll. Just tries to find a dll matching the longest
    // possible name of the symbol. For example, if the symbol is Foo.Bar.Baz.MyMethod,
    // we try to find dll names in order: Foo.Bar.Baz.dll, Foo.Bar.dll, Foo.dll. This can
    // probably be improved but works for simple cases.
    private static string? FindDllPath(string binariesPath, string symbol)
    {
        var str = symbol;
        while (str != null)
        {
            var path = Path.Combine(binariesPath, $"{str}.dll");
            if (File.Exists(path))
            {
                return path;
            }

            str = RemoveSymbolTail(str);
        }

        return null;
    }

    private static MetadataReader? GetMetadataReader(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var peReader = new PEReader(fs);
        if (!peReader.HasMetadata)
        {
            Console.Error.WriteLine("Image does not contain .NET metadata");
            return null;
        }

        peReader.TryOpenAssociatedPortablePdb(
            path,
            ReadFileIfExists,
            out var pdbReaderProvider,
            out _
        );
        if (pdbReaderProvider == null)
        {
            Console.Error.WriteLine($"Could not find associated pdb file for {path}");
            return null;
        }

        return pdbReaderProvider.GetMetadataReader();
    }

    private static FileStream? ReadFileIfExists(string fileName)
    {
        return File.Exists(fileName) ? File.OpenRead(fileName) : null;
    }
}
