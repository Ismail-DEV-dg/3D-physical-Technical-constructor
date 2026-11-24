using System.Windows;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.App.Services;

internal sealed class ApplicationErrorService(IErrorLogRepository log, IDialogService dialogs) : IApplicationErrorService
{
    private int reporting;
    private Func<bool>? saveRecoveryCopy;
    public void SetRecoveryAction(Func<bool> saveCopy) => saveRecoveryCopy = saveCopy;

    public void Report(Exception error)
    {
        if (error is OutOfMemoryException || Interlocked.Exchange(ref reporting, 1) != 0) return;
        var written = log.TryAppend(error);
        var app = Application.Current;
        if (app is null || app.Dispatcher.HasShutdownStarted) { Volatile.Write(ref reporting, 0); return; }

        // Queue the dialog so the failing input or binding can unwind first.
        _ = app.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (saveRecoveryCopy is { } save && dialogs.OfferCrashCopy(written) && save())
                    app.Shutdown();
            }
            finally { Volatile.Write(ref reporting, 0); }
        });
    }
}
