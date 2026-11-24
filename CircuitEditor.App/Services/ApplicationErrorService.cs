using System.Windows;
using CircuitEditor.App.ViewModels;
using CircuitEditor.Core.Repositories;

namespace CircuitEditor.App.Services;

internal sealed class ApplicationErrorService(IErrorLogRepository log, IDialogService dialogs,
    ProjectSessionViewModel session) : IApplicationErrorService
{
    private int reporting;

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
                if (dialogs.OfferCrashCopy(written) && session.SaveRecoveryCopy())
                    app.Shutdown();
            }
            finally { Volatile.Write(ref reporting, 0); }
        });
    }
}
