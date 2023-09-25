using Crash;

int Main(string[] args)
{
#if IOS
    AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
    {
        if (eventArgs.ExceptionObject is Exception e)
        {
            ReportCrash(e);
        }
    };

    // Needed to get exceptions with managed stack traces.
    // See https://github.com/xamarin/xamarin-macios/issues/15252
    ObjCRuntime.Runtime.MarshalManagedException += (_, args) =>
    {
        args.ExceptionMode = ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode;
    };
#elif ANDROID
    AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
    {
        ReportDecoratedStackTrace(args.Exception);
    };
#else
    AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
    {
        if (eventArgs.ExceptionObject is Exception e)
        {
            ReportCrash(e);
        }
    };
#endif

    Console.WriteLine("Does not crash");

    var crashing = new CrashingClass();
    crashing.CallThisToCrash();

    Console.WriteLine("Also does not crash");

    return 0;

    void ReportCrash(Exception e)
    {
        var stackTrace = StackTraceDecorator.Decorate(e);
        if (stackTrace == null)
        {
            // You might want to send the original stack trace here in
            // in case it can't be decorated.
            Console.WriteLine($"Undecorated stack trace:\n{e.StackTrace}");
            return;
        }

        // You might want to add some build releated info to the stack trace
        // to be able to pick the right symbols when symbolicating later.
        //
        // For example with Maui, net-ios or net-android:
        //
        // try
        // {
        //     var platform = DeviceInfo.Platform == DevicePlatform.iOS
        //         ? "ios"
        //         : "android";
        //     var buildInfo = $"{platform}-{AppInfo.PackageName}-{AppInfo.BuildString}\n";
        //     stackTrace = $"{buildInfo}{stackTrace}";
        // }
        // catch
        // {
        //     // Ignore.
        // }

        Console.WriteLine($"Decorated stack trace. {e.GetType().FullName}: {e.Message}\n{stackTrace}");

        // For App Center use, there is no way to get the original stack
        // trace decorated, but we can send an additional error report
        // with the decorated trace as an attachment:
        //
        // var attachment = ErrorAttachmentLog.AttachmentWithText(stackTrace, $"stacktrace.txt");
        // var attachments = new[] { attachment };
        //
        // Crashes.TrackError(e, null, attachments);
    }
}

return Main(args);
