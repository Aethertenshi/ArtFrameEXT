using System;
using System.Diagnostics;
using System.Threading;

public class HighPrecisionLimiter
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _targetFrameTimeMs = 1000.0 / 400.0; // E.g., 2.5 ms for 400 FPS
    private double _lastFrameTimeMs = 0;

    public void SetMaxFps(double maxFps)
    {
        _targetFrameTimeMs = 1000.0 / maxFps;
    }

    public void Wait()
    {
        double targetTime = _lastFrameTimeMs + _targetFrameTimeMs;

        while (true)
        {
            double elapsed = _stopwatch.Elapsed.TotalMilliseconds;
            double remaining = targetTime - elapsed;

            // 1. Coarse wait: Sleep if we have plenty of time left (saves CPU)
            if (remaining > 2.0)
            {
                Thread.Sleep(1);
                continue;
            }

            // 2. Fine wait: Spin-wait for extreme sub-millisecond precision
            if (remaining > 0)
            {
                // Thread.SpinWait(1) yields slightly to CPU hyperthreads
                // but keeps our thread active on the OS scheduler
                Thread.SpinWait(1);
                continue;
            }

            break; // Target time reached!
        }

        _lastFrameTimeMs = _stopwatch.Elapsed.TotalMilliseconds;
    }
}