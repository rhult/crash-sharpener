# ✏️ Crash Sharpener

Crash Sharpener helps you get strack traces with line numbers for mobile apps written with .NET. It works with apps build with the mobile app SDKs for iOS and Android, including Maui. The process of doing this is usually called to *symbolicate* a stack trace.

## 👩‍💻 How it works

There are three parts:

1. A piece of code that, when added to your app, decorates stack traces with information about where the crash happened.
2. The app sending the stack trace somewhere (a logging service, App Center, etc).
3. A tool that can take a decorated trace and map it to the right source file and line number.

Crash Sharpener helps with parts 1 and 3.

## 🖼️ Add the stack trace decorator to your app

Copy the file `example/StackTraceDecorator.cs` into your project. The method `StackTraceDecorator.Decorate()` will return a decorated stack trace. If you pass an exception, its stack trace will be used. If exception is `null`, the stack trace for the current stack frame will be used.

The file `example/Program.cs` shows how to hook up the exception handler and print the decorated stack trace to the console. It also has (commented out) code that suggests a way to report the crash to App Center. You most likely will need to modify this to suite your own needs, for example to send the reports to your own service.

## 🔎 Get useful stack traces

Put your decorated stack trace in a file. It should look something like this:

```
Decorated stack trace. System.DivideByZeroException: Attempted to divide by zero.
    at Crash.CrashingClass.TheCrash(Int32 a) IL_000e T_0600000b
    at Crash.CrashingClass.Is() IL_0001 T_0600000a
    at Crash.CrashingClass.This() IL_0001 T_06000009
    at Crash.CrashingClass.CallThisToCrash() IL_0001 T_06000008
    at Program+<>c__DisplayClass0_0.<<Main>$>g__Main|0(String[] args) IL_0029 T_06000010
    at Program.<Main>$(String[] args) IL_000e T_06000006
```

Now you can use the sharper tool to get more information:

```
sharpener example/bin/Release/net7.0 stacktrace.txt
```

The first command line argument is a path pointing to a directory with the dll and pdb files for the right build of the crashing app. The second argument is a text file containing the stack trace.

The output should look something like this:

```
Decorated stack trace. System.DivideByZeroException: Attempted to divide by zero.
    at Crash.CrashingClass.TheCrash(Int32 a) in example/CrashingClass.cs:23 [23:9-23:42]
    at Crash.CrashingClass.Is() in example/CrashingClass.cs:17 [17:9-17:22]
    at Crash.CrashingClass.This() in example/CrashingClass.cs:12 [12:9-12:14]
    at Crash.CrashingClass.CallThisToCrash() in example/CrashingClass.cs:7 [7:9-7:16]
    at Program.<<Main>$>g__Main|0_0(String[] args) IL_003c T_06000008
    at Program.<Main>$(String[] args) IL_0001 T_06000006
```

The original line will be shown if a symbol can't be resolved. The range in brackets represents the start and end column.

## 🔧 Convenience wrapper script

The simple way of running the tool described above might work for many cases. However, if you have many builds and stack traces, it helps to save dlls and pdbs for each build in a directory somewhere. The bash script `sharpener.sh` in the root of the repository can help you juggle all the builds more easily. To use it, follow those steps:

Put dlls and pdbs in a versioned structure like this:

```
symbols/
  ios-com.example.myapp-1002/
    myapp.dll
    mylib.dll
    myapp.pdb
    myapp.pdb
    ...
  android-com.example.myapp-1023/
    myapp.dll
    mylib.dll
    myapp.pdb
    myapp.pdb
    ...
```

Make sure you add a line at the top of your decorated stack traces with the matching directory name. This is something you can do in your exception handler in the app (see the code example for how to do this with Maui). For example:

```
ios-com.example.myapp-1002
    at Crash.CrashingClass.TheCrash(Int32 a) IL_000e T_0600000b
    at Crash.CrashingClass.Is() IL_0001 T_0600000a
    at Crash.CrashingClass.This() IL_0001 T_06000009
    at Crash.CrashingClass.CallThisToCrash() IL_0001 T_06000008
```

Run `sharpener.sh`:

`./sharpener.sh /path/to/symbols stacktrace.txt`

The script will look in the right directory and symbolicate the stack trace with debug information from the right build.

## 🤷‍♀️ What about App Center and other services?

App Center and many other crash reporting tools do not support .NET stack traces well,  or at all. You can still use App Center for crash reporting by creating your own error reports there. Check out the code under the `example` directory for a simple example of doing this.

If you are ambitious, you might want to set up a small server that keeps the debug information for all your release builds and then automatically symbolicates stack traces sent to it. Please let me know if you do anything interesting with this tool.

## ⚙️ Compatibility

I have tested this mainly with .NET 7, but it should work on .NET 6 and up, maybe with some small adjustments.
