namespace Crash;

public class CrashingClass
{
    public void CallThisToCrash()
    {
        This();
    }

    private void This()
    {
        Is();
    }

    private void Is()
    {
        TheCrash(42);
    }

    private void TheCrash(int a)
    {
        var zero = a - a;
        Console.WriteLine($"{1 / zero}");
    }
}
