using System;
using System.Runtime.InteropServices;

namespace Art2Core.SdlBindings
{
    public enum WindowMode
    {
        Resizable,
        Fixed,
        Fullscreen,
    }

    /// <summary>
    /// Static class for managing SDL3 window creation and event processing.
    /// </summary>
    public static partial class Window
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

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool SDL_Init(uint flags);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial void SDL_Quit();

        [LibraryImport(DllName, EntryPoint = "SDL_CreateWindow", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial IntPtr SDL_CreateWindow(string title, int w, int h, ulong flags);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial void SDL_DestroyWindow(IntPtr window);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool SDL_PollEvent(out SDL_Event ev);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial IntPtr SDL_GetError();

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
        public static bool Create(string title, int width, int height, WindowMode resizable = WindowMode.Resizable)
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
                if (!SdlTtf.TTF_Init())
                {
                    string error = GetLastError();
                    Console.WriteLine($"Failed to initialize SDL_ttf: {error}");
                    SDL_Quit();
                    return false;
                }
                _isInitialized = true;
            }

            ulong flags = 0;
            switch (resizable)
            {
                case WindowMode.Resizable:
                    flags = SDL_WINDOW_RESIZABLE;
                    break;
                case WindowMode.Fullscreen:
                    flags = 0x0000000000000001ul; // SDL_WINDOW_FULLSCREEN
                    break;
                case WindowMode.Fixed:
                    break;
                default:
                    break;
            }

            _windowPtr = SDL_CreateWindow(title, width, height, flags);

            if (_windowPtr == IntPtr.Zero)
            {
                string error = GetLastError();
                Console.WriteLine($"Failed to create window: {error}");
                SdlTtf.TTF_Quit();
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
                SdlTtf.TTF_Quit();
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