using System;
using System.Runtime.InteropServices;

namespace Art2Core.SdlBindings
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

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_Rect
    {
        public int x;
        public int y;
        public int w;
        public int h;

        public SDL_Rect(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }
    }

    public enum SDL_GPUShaderFormat : uint
    {
        INVALID = 0,
        PRIVATE = (1 << 0),  // Metal MSL source
        SPIRV = (1 << 1),    // Vulkan SPIR-V
        DXBC = (1 << 2),     // D3D11 HLSL DXBC (SM 4.0/5.0)
        DXIL = (1 << 3),     // D3D12 HLSL DXIL
        MSL = (1 << 4)       // Metal MSL bytecode
    }

    public enum SDL_GPUShaderStage : uint
    {
        VERTEX = 0,
        FRAGMENT = 1,
        COMPUTE = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_GPUShaderCreateInfo
    {
        public uint code_size;
        public IntPtr code;
        public IntPtr entrypoint;
        public SDL_GPUShaderFormat format;
        public SDL_GPUShaderStage stage;
        public uint num_samplers;
        public uint num_storage_textures;
        public uint num_storage_buffers;
        public uint num_uniform_buffers;
        public uint props;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_GPURenderStateCreateInfo
    {
        public IntPtr fragment_shader; // Pointer to SDL_GPUShader
        public int num_sampler_bindings;
    }

    /// <summary>
    /// Static class handling the 2D hardware-accelerated rendering and geometry batching.
    /// </summary>
    public static partial class Renderer
    {
        private const string DllName = "SDL3";

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial IntPtr SDL_CreateGPURenderer(IntPtr device, IntPtr window);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial void SDL_DestroyRenderer(IntPtr renderer);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool SDL_RenderClear(IntPtr renderer);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool SDL_RenderPresent(IntPtr renderer);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool SDL_SetRenderDrawColor(IntPtr renderer, byte r, byte g, byte b, byte a);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static unsafe partial bool SDL_RenderGeometry(
            IntPtr renderer, 
            IntPtr texture, 
            SDL_Vertex* vertices, 
            int num_vertices, 
            int* indices, 
            int num_indices);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial IntPtr SDL_GetRendererName(IntPtr renderer);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool SDL_RenderTexture(IntPtr renderer, IntPtr texture, IntPtr srcrect, ref SDL_FRect dstrect);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial void SDL_DestroyTexture(IntPtr texture);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr SDL_GetGPURendererDevice(IntPtr renderer);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr SDL_CreateGPUShader(IntPtr device, ref SDL_GPUShaderCreateInfo createinfo);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr SDL_CreateGPURenderState(IntPtr renderer, ref SDL_GPURenderStateCreateInfo createinfo);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool SDL_SetGPURenderState(IntPtr renderer, IntPtr state);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void SDL_DestroyGPURenderState(IntPtr renderer, IntPtr state);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void SDL_ReleaseGPUShader(IntPtr device, IntPtr shader);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr SDL_CreateTextureFromSurface(IntPtr renderer, IntPtr surface);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void SDL_DestroySurface(IntPtr surface);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr SDL_CreateSurface(int width, int height, uint format);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool SDL_BlitSurface(IntPtr src, IntPtr srcrect, IntPtr dst, ref SDL_Rect dstrect);

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
        private static IntPtr _currentTexture = IntPtr.Zero;
        private static IntPtr _currentShaderState = IntPtr.Zero;

        /// <summary>
        /// Initializes the renderer for a specific window.
        /// </summary>
        public static bool Initialize(IntPtr window)
        {
            if (_rendererPtr != IntPtr.Zero)
            {
                return true;
            }

            // Create a GPU-backed renderer so custom fragment shaders are supported!
            _rendererPtr = SDL_CreateGPURenderer(IntPtr.Zero, window);
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
            _currentTexture = IntPtr.Zero;
            _currentShaderState = IntPtr.Zero;

            SDL_SetRenderDrawColor(_rendererPtr, clearR, clearG, clearB, clearA);
            SDL_RenderClear(_rendererPtr);
        }

        /// <summary>
        /// Queues a colored quad to be rendered in the batch.
        /// </summary>
        public static void DrawQuad(float x, float y, float w, float h, SDL_FColor color)
        {
            if (_currentTexture != IntPtr.Zero || _quadCount >= MaxQuads)
            {
                Flush();
                _currentTexture = IntPtr.Zero;
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
        /// Queues a textured quad to be rendered in the batch, automatically flushing if the texture changes.
        /// </summary>
        /// <param name="texture">The native SDL_Texture pointer to render.</param>
        /// <param name="x">The screen X coordinate.</param>
        /// <param name="y">The screen Y coordinate.</param>
        /// <param name="w">The width to render.</param>
        /// <param name="h">The height to render.</param>
        /// <param name="u1">The left UV coordinate.</param>
        /// <param name="v1">The top UV coordinate.</param>
        /// <param name="u2">The right UV coordinate.</param>
        /// <param name="v2">The bottom UV coordinate.</param>
        /// <param name="color">The color modulation to apply.</param>
        public static void DrawTextureQuad(IntPtr texture, float x, float y, float w, float h, float u1, float v1, float u2, float v2, SDL_FColor color)
        {
            if (texture != _currentTexture || _quadCount >= MaxQuads)
            {
                Flush();
                _currentTexture = texture;
            }

            int vOffset = _quadCount * 4;

            // 1. Top-Left
            _vertexBuffer[vOffset + 0].position = new SDL_FPoint(x, y);
            _vertexBuffer[vOffset + 0].color = color;
            _vertexBuffer[vOffset + 0].tex_coord = new SDL_FPoint(u1, v1);

            // 2. Top-Right
            _vertexBuffer[vOffset + 1].position = new SDL_FPoint(x + w, y);
            _vertexBuffer[vOffset + 1].color = color;
            _vertexBuffer[vOffset + 1].tex_coord = new SDL_FPoint(u2, v1);

            // 3. Bottom-Right
            _vertexBuffer[vOffset + 2].position = new SDL_FPoint(x + w, y + h);
            _vertexBuffer[vOffset + 2].color = color;
            _vertexBuffer[vOffset + 2].tex_coord = new SDL_FPoint(u2, v2);

            // 4. Bottom-Left
            _vertexBuffer[vOffset + 3].position = new SDL_FPoint(x, y + h);
            _vertexBuffer[vOffset + 3].color = color;
            _vertexBuffer[vOffset + 3].tex_coord = new SDL_FPoint(u1, v2);

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
            
            // Render all accumulated geometry as a single hardware-accelerated batch using unsafe fixed pinning
            unsafe
            {
                fixed (SDL_Vertex* pVertices = _vertexBuffer)
                {
                    fixed (int* pIndices = _indexBuffer)
                    {
                        SDL_RenderGeometry(_rendererPtr, _currentTexture, pVertices, vertexCount, pIndices, indexCount);
                    }
                }
            }

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
        /// Applies a custom GPU render state (fragment shader) to subsequent batched draw calls.
        /// </summary>
        public static void SetShader(IntPtr renderState)
        {
            if (_currentShaderState != renderState)
            {
                Flush(); // Flush all previous drawings under the old shader first!
                _currentShaderState = renderState;
                SDL_SetGPURenderState(_rendererPtr, renderState);
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
