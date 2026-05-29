using System;
using System.Runtime.InteropServices;

namespace ArtFrameCore.SdlBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_FPoint
    {
        public float x;
        public float y;

        public SDL_FPoint(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_FColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public SDL_FColor(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_Vertex
    {
        public SDL_FPoint position;
        public SDL_FColor color;
        public SDL_FPoint tex_coord;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_FRect
    {
        public float x;
        public float y;
        public float w;
        public float h;

        public SDL_FRect(float x, float y, float w, float h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }
    }

    /// <summary>
    /// Static class handling the 2D hardware-accelerated rendering and geometry batching.
    /// </summary>
    public static class Renderer
    {
        private const string DllName = "SDL3.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_CreateRenderer(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string? name);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_DestroyRenderer(IntPtr renderer);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SDL_RenderClear(IntPtr renderer);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SDL_RenderPresent(IntPtr renderer);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SDL_SetRenderDrawColor(IntPtr renderer, byte r, byte g, byte b, byte a);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SDL_RenderGeometry(
            IntPtr renderer, 
            IntPtr texture, 
            [In] SDL_Vertex[] vertices, 
            int num_vertices, 
            [In] int[]? indices, 
            int num_indices);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetRendererName(IntPtr renderer);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SDL_RenderTexture(IntPtr renderer, IntPtr texture, IntPtr srcrect, ref SDL_FRect dstrect);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_DestroyTexture(IntPtr texture);

        private static IntPtr _rendererPtr = IntPtr.Zero;

        /// <summary>
        /// Gets the raw SDL3 renderer pointer.
        /// </summary>
        public static IntPtr Pointer => _rendererPtr;

        /// <summary>
        /// Gets the name of the active rendering backend (e.g. "direct3d11", "opengl", etc.).
        /// </summary>
        public static string GetName()
        {
            if (_rendererPtr == IntPtr.Zero)
            {
                return "Not Initialized";
            }
            IntPtr namePtr = SDL_GetRendererName(_rendererPtr);
            return Marshal.PtrToStringUTF8(namePtr) ?? "Unknown";
        }
        
        // Batching fields
        private const int MaxQuads = 1024;
        private const int MaxVertices = MaxQuads * 4;
        private const int MaxIndices = MaxQuads * 6;

        private static readonly SDL_Vertex[] _vertexBuffer = new SDL_Vertex[MaxVertices];
        private static readonly int[] _indexBuffer = new int[MaxIndices];
        private static int _quadCount = 0;

        /// <summary>
        /// Initializes the renderer for a specific window.
        /// </summary>
        public static bool Initialize(IntPtr window)
        {
            if (_rendererPtr != IntPtr.Zero)
            {
                return true;
            }

            // In SDL3, we pass null to automatically select the best graphics driver
            _rendererPtr = SDL_CreateRenderer(window, null);
            if (_rendererPtr == IntPtr.Zero)
            {
                Console.WriteLine("Failed to create SDL renderer.");
                return false;
            }

            // Pre-generate the static index pattern: [0,1,2, 0,2,3, 4,5,6, 4,6,7, ...]
            for (int i = 0; i < MaxQuads; i++)
            {
                int vOffset = i * 4;
                int iOffset = i * 6;

                _indexBuffer[iOffset + 0] = vOffset + 0;
                _indexBuffer[iOffset + 1] = vOffset + 1;
                _indexBuffer[iOffset + 2] = vOffset + 2;
                _indexBuffer[iOffset + 3] = vOffset + 0;
                _indexBuffer[iOffset + 4] = vOffset + 2;
                _indexBuffer[iOffset + 5] = vOffset + 3;
            }

            return true;
        }

        /// <summary>
        /// Begins a new frame and clears the screen to a specific color.
        /// </summary>
        public static void BeginFrame(byte clearR = 30, byte clearG = 30, byte clearB = 40, byte clearA = 255)
        {
            if (_rendererPtr == IntPtr.Zero) return;

            _quadCount = 0;

            SDL_SetRenderDrawColor(_rendererPtr, clearR, clearG, clearB, clearA);
            SDL_RenderClear(_rendererPtr);
        }

        /// <summary>
        /// Queues a colored quad to be rendered in the batch.
        /// </summary>
        public static void DrawQuad(float x, float y, float w, float h, SDL_FColor color)
        {
            if (_quadCount >= MaxQuads)
            {
                Flush();
            }

            int vOffset = _quadCount * 4;

            // 1. Top-Left
            _vertexBuffer[vOffset + 0].position = new SDL_FPoint(x, y);
            _vertexBuffer[vOffset + 0].color = color;
            _vertexBuffer[vOffset + 0].tex_coord = new SDL_FPoint(0, 0);

            // 2. Top-Right
            _vertexBuffer[vOffset + 1].position = new SDL_FPoint(x + w, y);
            _vertexBuffer[vOffset + 1].color = color;
            _vertexBuffer[vOffset + 1].tex_coord = new SDL_FPoint(1, 0);

            // 3. Bottom-Right
            _vertexBuffer[vOffset + 2].position = new SDL_FPoint(x + w, y + h);
            _vertexBuffer[vOffset + 2].color = color;
            _vertexBuffer[vOffset + 2].tex_coord = new SDL_FPoint(1, 1);

            // 4. Bottom-Left
            _vertexBuffer[vOffset + 3].position = new SDL_FPoint(x, y + h);
            _vertexBuffer[vOffset + 3].color = color;
            _vertexBuffer[vOffset + 3].tex_coord = new SDL_FPoint(0, 1);

            _quadCount++;
        }

        /// <summary>
        /// Flushes the active geometry to the GPU in a single draw call.
        /// </summary>
        public static void Flush()
        {
            if (_rendererPtr == IntPtr.Zero || _quadCount == 0) return;

            int vertexCount = _quadCount * 4;
            int indexCount = _quadCount * 6;

            // Render all accumulated vertices as a single batch!
            SDL_RenderGeometry(_rendererPtr, IntPtr.Zero, _vertexBuffer, vertexCount, _indexBuffer, indexCount);

            _quadCount = 0;
        }

        /// <summary>
        /// Flushes remaining batched items and presents the backbuffer to the screen.
        /// </summary>
        public static void EndFrame()
        {
            if (_rendererPtr == IntPtr.Zero) return;

            Flush();
            SDL_RenderPresent(_rendererPtr);
        }

        /// <summary>
        /// Draws a texture at the specified position and dimensions.
        /// </summary>
        public static void DrawTexture(IntPtr texture, float x, float y, float w, float h)
        {
            if (_rendererPtr == IntPtr.Zero || texture == IntPtr.Zero) return;

            // We must flush any batched geometry first, to maintain correct draw order!
            Flush();

            var dst = new SDL_FRect(x, y, w, h);
            SDL_RenderTexture(_rendererPtr, texture, IntPtr.Zero, ref dst);
        }

        /// <summary>
        /// Destroys a texture resource.
        /// </summary>
        public static void DestroyTexture(IntPtr texture)
        {
            if (texture != IntPtr.Zero)
            {
                SDL_DestroyTexture(texture);
            }
        }

        /// <summary>
        /// Cleans up the renderer resource.
        /// </summary>
        public static void Shutdown()
        {
            if (_rendererPtr != IntPtr.Zero)
            {
                SDL_DestroyRenderer(_rendererPtr);
                _rendererPtr = IntPtr.Zero;
            }
        }
    }
}
