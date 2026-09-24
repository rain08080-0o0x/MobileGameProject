using System;

public interface ITraceDrawingSource
{
    event Action<TraceStrokeResult[]> BatchCompleted;
    event Action<TraceStrokeResult> StrokeScored;
    event Action TracingAvailable;
    event Action TracingCompleted;

    int CompletedCount { get; }
    int TargetCount { get; }
    float CurrentDrawingSeconds { get; }

    void BeginRound();
    void SetInputEnabled(bool enabled);
    void ClearCollectedLines();
}
