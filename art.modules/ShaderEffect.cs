using System;
using System.IO;
using System.Runtime.InteropServices;
using ArtFrameCore.SdlBindings;

namespace ArtFrameCore.Modules
{
    /// <summary>
    /// Represents a custom hardware-accelerated GPU Fragment Shader and its active Renderer State.
    /// </summary>
    public class ShaderEffect : IDisposable
    {
        private IntPtr _shaderPtr = IntPtr.Zero;
        private IntPtr _renderStatePtr = IntPtr.Zero;

        /// <summary>
        /// Gets the raw native pointer to the compiled SDL_GPURenderState.
        /// </summary>
        public IntPtr RenderStatePointer => _renderStatePtr;

        /// <summary>
        /// Loads, compiles, and registers a pre-compiled GPU fragment shader binary (e.g. SPIR-V, DXBC) into the renderer.
        /// </summary>
        /// <param name="shaderBinaryPath">The filesystem path to the compiled shader bytecode binary.</param>
        /// <param name="format">The graphics format of the shader bytecode (e.g., SPIRV, DXBC).</param>
        /// <param name="entrypoint">The entrypoint function name within the shader code (defaults to "main").</param>
        public ShaderEffect(string shaderBinaryPath, SDL_GPUShaderFormat format, string entrypoint = "main")
        {
            if (string.IsNullOrEmpty(shaderBinaryPath)) throw new ArgumentNullException(nameof(shaderBinaryPath));

            IntPtr renderer = Renderer.Pointer;
            if (renderer == IntPtr.Zero)
            {
                throw new InvalidOperationException("[ArtFrameCore] Cannot create shader: SDL Renderer is not initialized.");
            }

            IntPtr device = Renderer.SDL_GetGPURendererDevice(renderer);
            if (device == IntPtr.Zero)
            {
                throw new NotSupportedException("[ArtFrameCore] Active renderer backend does not support GPU shader states.");
            }

            byte[] shaderBytes = File.ReadAllBytes(shaderBinaryPath);
            GCHandle pinnedArray = GCHandle.Alloc(shaderBytes, GCHandleType.Pinned);

            try
            {
                var shaderInfo = new SDL_GPUShaderCreateInfo
                {
                    code_size = (uint)shaderBytes.Length,
                    code = pinnedArray.AddrOfPinnedObject(),
                    entrypoint = entrypoint,
                    format = format,
                    stage = SDL_GPUShaderStage.FRAGMENT,
                    num_samplers = 1, // At least 1 texture sampler bound by default
                    num_storage_textures = 0,
                    num_storage_buffers = 0,
                    num_uniform_buffers = 0,
                    props = 0
                };

                _shaderPtr = Renderer.SDL_CreateGPUShader(device, ref shaderInfo);
                if (_shaderPtr == IntPtr.Zero)
                {
                    throw new Exception($"[ArtFrameCore] Failed to compile GPU Shader: {Marshal.PtrToStringUTF8(SDL3_GetError())}");
                }

                var stateInfo = new SDL_GPURenderStateCreateInfo
                {
                    fragment_shader = _shaderPtr,
                    num_sampler_bindings = 0
                };

                _renderStatePtr = Renderer.SDL_CreateGPURenderState(renderer, ref stateInfo);
                if (_renderStatePtr == IntPtr.Zero)
                {
                    throw new Exception("[ArtFrameCore] Failed to instantiate GPU Render State.");
                }
            }
            finally
            {
                pinnedArray.Free();
            }
        }

        [DllImport("SDL3.dll", EntryPoint = "SDL_GetError", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL3_GetError();

        /// <summary>
        /// Releases all allocated native GPU resources.
        /// </summary>
        public void Dispose()
        {
            IntPtr renderer = Renderer.Pointer;
            if (renderer != IntPtr.Zero && _renderStatePtr != IntPtr.Zero)
            {
                Renderer.SDL_DestroyGPURenderState(renderer, _renderStatePtr);
                _renderStatePtr = IntPtr.Zero;
            }

            IntPtr device = Renderer.SDL_GetGPURendererDevice(renderer);
            if (device != IntPtr.Zero && _shaderPtr != IntPtr.Zero)
            {
                Renderer.SDL_ReleaseGPUShader(device, _shaderPtr);
                _shaderPtr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer for the ShaderEffect resource.
        /// </summary>
        ~ShaderEffect()
        {
            Dispose();
        }
    }
}
