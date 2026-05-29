using System;
using System.Runtime.InteropServices;

namespace ArtFrameCore.SdlBindings
{
    /// <summary>
    /// Static class for managing SDL3 window creation and event processing.
    /// </summary>
    public static class Window
    {
        private const string DllName = "SDL3.dll";

        private const uint SDL_INIT_VIDEO = 0x00000020u;
        private const ulong SDL_WINDOW_RESIZABLE = 0x0000000000000020ul;
        private const uint SDL_EVENT_QUIT = 0x100;

        [StructLayout(LayoutKind.Explicit, Size = 128)]
        private struct SDL_Event
        {
            [FieldOffset(0)]
            public uint type;
        }

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SDL_Init(uint flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_Quit();

        [DllImport(DllName, EntryPoint = "SDL_CreateWindow", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_CreateWindow([MarshalAs(UnmanagedType.LPUTF8Str)] string title, int w, int h, ulong flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_DestroyWindow(IntPtr window);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SDL_PollEvent(out SDL_Event ev);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetError();

        private static IntPtr _windowPtr = IntPtr.Zero;
        private static bool _isInitialized = false;

        /// <summary>
        /// Initializes the SDL video subsystem and creates a window.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="width">The width of the window.</param>
        /// <param name="height">The height of the window.</param>
        /// <param name="resizable">Whether the window is resizable.</param>
        /// <returns>True if the window was successfully created; otherwise, false.</returns>
        public static bool Create(string title, int width, int height, bool resizable = true)
        {
            if (_windowPtr != IntPtr.Zero)
            {
                Console.WriteLine("Window is already created.");
                return false;
            }

            if (!_isInitialized)
            {
                if (!SDL_Init(SDL_INIT_VIDEO))
                {
                    string error = GetLastError();
                    Console.WriteLine($"Failed to initialize SDL: {error}");
                    return false;
                }
                _isInitialized = true;
            }

            ulong flags = resizable ? SDL_WINDOW_RESIZABLE : 0;
            _windowPtr = SDL_CreateWindow(title, width, height, flags);

            if (_windowPtr == IntPtr.Zero)
            {
                string error = GetLastError();
                Console.WriteLine($"Failed to create window: {error}");
                SDL_Quit();
                _isInitialized = false;
                return false;
            }

            // Initialize the Renderer with our new window pointer
            if (!Renderer.Initialize(_windowPtr))
            {
                Close();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Polls for window events (like close request) and returns whether the window should continue running.
        /// </summary>
        /// <returns>True if the window is open and active; false if the user closed the window or it wasn't created.</returns>
        public static bool KeepRunning()
        {
            if (_windowPtr == IntPtr.Zero)
            {
                return false;
            }

            SDL_Event ev;
            while (SDL_PollEvent(out ev))
            {
                if (ev.type == SDL_EVENT_QUIT)
                {
                    Close();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Closes the window and cleans up SDL resources.
        /// </summary>
        public static void Close()
        {
            // Shutdown the renderer first
            Renderer.Shutdown();

            if (_windowPtr != IntPtr.Zero)
            {
                SDL_DestroyWindow(_windowPtr);
                _windowPtr = IntPtr.Zero;
            }

            if (_isInitialized)
            {
                SDL_Quit();
                _isInitialized = false;
            }
        }

        private static string GetLastError()
        {
            IntPtr errPtr = SDL_GetError();
            if (errPtr == IntPtr.Zero)
            {
                return "Unknown error";
            }
            return Marshal.PtrToStringUTF8(errPtr) ?? "Unknown error";
        }
    }
}