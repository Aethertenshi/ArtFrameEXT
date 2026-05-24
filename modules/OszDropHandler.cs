using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using SDL3;

namespace ArtFrame.FileProcessing;

/// <summary>
/// Hooks into SDL2's DROPFILE event (the native OS drag-and-drop mechanism)
/// and queues dropped .osz paths so the main thread can safely process them.
///
/// How it works:
///   SDL_AddEventWatch registers a C callback that fires for every SDL event
///   before FNA processes it.  We catch SDL_DROPFILE events there, copy the
///   file path into a thread-safe ConcurrentQueue, and let the game's Update()
///   drain the queue.
///
/// Setup (call once in Init/constructor):
///   OszDropHandler.Initialize();
///
/// Per-frame drain (call in Update):
///   OszDropHandler.DrainQueue(path => { /* import path */ });
/// </summary>
public static class OszDropHandler
{
    // Thread-safe path queue — the SDL callback runs on the SDL event thread.
    private static readonly ConcurrentQueue<string> _pending = new();

    // We must keep a GC-rooted reference to the delegate, otherwise the GC
    // will collect it while native code still holds a pointer → crash.
    private static SDL.SDL_EventFilter? _filterDelegate;

    private static bool _initialized = false;

    /// <summary>
    /// Registers the SDL drop-event watcher. Call once during game Init.
    /// Also enables SDL_DROPFILE events (some platforms need explicit opt-in).
    /// </summary>
    public static unsafe void Initialize()
    {
        if (_initialized) return;

        // Tell SDL to deliver DROPFILE events
        SDL.SDL_SetEventEnabled((uint)SDL.SDL_EventType.SDL_EVENT_DROP_FILE, true);

        // SDL_AddEventWatch fires for every event — we filter by type inside
        _filterDelegate = DropEventFilter;
        SDL.SDL_AddEventWatch(_filterDelegate, IntPtr.Zero);

        _initialized = true;
        Console.WriteLine("[OszDropHandler] Drag-and-drop initialized. Drop an .osz onto the window.");
    }

    private static unsafe bool DropEventFilter(nint userdata, SDL.SDL_Event* evt)
    {
        if (evt->type == (uint)SDL.SDL_EventType.SDL_EVENT_DROP_FILE)
        {
            // 2. In SDL3, the string pointer is stored in 'drop.data'
            IntPtr stringPtr = (IntPtr)evt->drop.data;

            // 3. Read the raw memory into a safe C# string
            string? path = Marshal.PtrToStringUTF8(stringPtr);

            if (!string.IsNullOrEmpty(path))
            {
                // 4. Push it to the thread-safe queue!
                _pending.Enqueue(path);
            }
        }

        // Return true to allow SDL to finish processing the event normally
        return true;
    }

    /// <summary>
    /// Drain queued file paths on the main thread.
    /// <paramref name="onFile"/> is called once per file with the full path.
    /// </summary>
    public static void DrainQueue(Action<string> onFile)
    {
        while (_pending.TryDequeue(out string? path))
        {
            if (path != null) onFile(path);
        }
    }

    // ── SDL callback ─────────────────────────────────────────────────────────
}