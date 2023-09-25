using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Crash;

// Copy this class into your project. It decorates the stack trace with
// IL offsets and method tokens so we can symbolicate it later.

public static class StackTraceDecorator
{
    public static string? Decorate(Exception? ex)
    {
        try
        {
            StackTrace stackTrace;

            if (ex != null)
            {
                stackTrace = new StackTrace(ex, false);
            }
            else
            {
                stackTrace = new StackTrace();
            }

            var stackFrames = stackTrace.GetFrames();
            if (stackFrames.Length == 0)
            {
                return null;
            }

            var sb = new StringBuilder();

            if (ex != null)
            {
                try
                {
                    sb.AppendLine($"{ex.GetType().FullName}: {ex.Message}");
                }
                catch
                {
                    // Ignore.
                }
            }

            foreach (var stackFrame in stackFrames)
            {
                var method = stackFrame.GetMethod();
                if (method == null)
                {
                    continue;
                }

                var typeFullName = method.ReflectedType?.FullName;
                if (typeFullName == null)
                {
                    continue;
                }

                TryGetMethodParameters(method, out var parameters);
                var offset = $"IL_{stackFrame.GetILOffset():x4}";
                var token = $"T_{method.MetadataToken:x8}";
                sb.AppendLine($"    at {typeFullName}.{method.Name}({parameters}) {offset} {token}");
            }

            return sb.ToString();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Could not decorate stack trace for exception: {e}");
            return null;
        }
    }

    private static bool TryGetMethodParameters(MethodBase method, out string parametersString)
    {
        try
        {
            var parameters = method.GetParameters();

            var length = parameters.Length;
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                var parameter = parameters[i];
                sb.Append(parameter.ParameterType.Name);
                if (parameter.Name?.Length > 0)
                {
                    sb.Append(" ");
                    sb.Append(parameter.Name);
                }
                if (i < length - 1)
                {
                    sb.Append(", ");
                }
            }

            parametersString = sb.ToString();
            return true;
        }
        catch
        {
            // If we can't get the parameters, ignore them.
        }

        parametersString = null;
        return false;
    }
}
