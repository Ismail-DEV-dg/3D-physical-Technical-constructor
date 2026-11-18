using System.Runtime.ExceptionServices;

namespace CircuitEditor.Tests;

internal static class StaRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    public static void Run(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception exception) { failure = exception; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(Timeout), "WPF resource verification timed out.");
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
