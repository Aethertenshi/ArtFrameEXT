using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public static class RealTimeInputEngine
{
    private static Thread _inputThread;
    private static bool _running;
    private static readonly HighPrecisionLimiter _limiter = new HighPrecisionLimiter();
    private static readonly Stopwatch _sw = Stopwatch.StartNew();
    private static int _tickCount = 0;
    private static float _currentHz = 0f;
    private static double _hzTimer = 0;

    public static float CurrentHz => _currentHz;

    private static readonly object _lock = new object();
    private static int[] _trackedVirtualKeys = new int[0];
    private static bool[] _lastRawStates = new bool[0];

    // Accumulated counts and state for the main thread
    private static int _accumulatedPresses = 0;
    private static bool _anyKeyCurrentlyHeld = false;
    public static long LatestTimestampMs { get; private set; }

    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // Nested helper class to isolate Win32 P/Invoke and prevent JIT errors on non-Windows platforms.
    private static class WindowsNative
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }

    public static void Start()
    {
        if (_running) return;
        _running = true;
        _limiter.SetMaxFps(600);

        _inputThread = new Thread(Loop)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest,
        };
        _inputThread.Start();
    }

    // Configures the engine with raw integer virtual keys
    public static void ConfigureKeys(int[] virtualKeys)
    {
        lock (_lock)
        {
            _trackedVirtualKeys = virtualKeys;
            _lastRawStates = new bool[virtualKeys.Length];
            _accumulatedPresses = 0;
            _anyKeyCurrentlyHeld = false;
        }
    }

    private static void Loop()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        double lastTime = stopwatch.Elapsed.TotalSeconds;

        while (_running)
        {
            lock (_lock)
            {
                if (_trackedVirtualKeys.Length == 0)
                {
                    _limiter.Wait();
                    continue;
                }

                bool structuralAnyHeld = false;
                long currentTicks = _sw.ElapsedMilliseconds;

                // On non-Windows platforms, fetch standard FNA KeyboardState safely once per loop tick
                Microsoft.Xna.Framework.Input.KeyboardState? fallbackState = null;
                if (!IsWindows)
                {
                    try
                    {
                        fallbackState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                    }
                    catch
                    {
                        // Fallback in case of headless or test environments where FNA isn't fully initialized
                    }
                }

                for (int i = 0; i < _trackedVirtualKeys.Length; i++)
                {
                    bool isDown = false;
                    if (IsWindows)
                    {
                        try
                        {
                            isDown = (WindowsNative.GetAsyncKeyState(_trackedVirtualKeys[i]) & 0x8000) != 0;
                        }
                        catch
                        {
                            isDown = false;
                        }
                    }
                    else if (fallbackState.HasValue)
                    {
                        isDown = fallbackState.Value.IsKeyDown((Microsoft.Xna.Framework.Input.Keys)_trackedVirtualKeys[i]);
                    }

                    // Edge detection: Transitioned from UP to DOWN since last tick
                    if (isDown && !_lastRawStates[i])
                    {
                        _accumulatedPresses++;
                        LatestTimestampMs = currentTicks;
                    }

                    _lastRawStates[i] = isDown;
                    if (isDown) structuralAnyHeld = true;
                }

                _anyKeyCurrentlyHeld = structuralAnyHeld;
            }

            _tickCount++;
            double currentTime = stopwatch.Elapsed.TotalSeconds;
            double elapsed = currentTime - lastTime;

            _hzTimer += elapsed;
            lastTime = currentTime;

            if (_hzTimer >= 0.25) // Update readout 4 times a second
            {
                _currentHz = (float)(_tickCount / _hzTimer);
                _tickCount = 0;
                _hzTimer = 0;
            }

            _limiter.Wait();
        }
    }

    // Called once per frame by the main thread to consume collected taps
    public static int ConsumePressCount()
    {
        lock (_lock)
        {
            int count = _accumulatedPresses;
            _accumulatedPresses = 0; // Reset counter for the next frame
            return count;
        }
    }

    // Called by hold/slider logic to see if any key is currently active
    public static bool IsAnyKeyHeld()
    {
        lock (_lock)
        {
            return _anyKeyCurrentlyHeld;
        }
    }

    public static void Stop()
    {
        _running = false;
        _inputThread?.Join();
    }
}