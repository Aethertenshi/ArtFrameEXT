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
            // On Linux/Ubuntu, Thread.Sleep(1) has a minimum granularity of ~4-10ms,
            // so we use a larger sleep threshold on non-Windows platforms to prevent capping the loop.
            double sleepThreshold = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ? 2.0 : 10.0;
            if (remaining > sleepThreshold)
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