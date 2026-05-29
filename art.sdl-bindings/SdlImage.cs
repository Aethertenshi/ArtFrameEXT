using System;
using System.Runtime.InteropServices;

namespace ArtFrameCore.SdlBindings
{
    /// <summary>
    /// Static class handling native bindings for the SDL3_image.dll library.
    /// </summary>
    public static class SdlImage
    {
        private const string DllName = "SDL3_image.dll";

        /// <summary>
        /// Loads an image from a filesystem path directly into an hardware-accelerated SDL_Texture.
        /// </summary>
        /// <param name="renderer">The rendering context pointer.</param>
        /// <param name="file">The filesystem path to the image file (UTF-8 format).</param>
        /// <returns>A pointer to the created SDL_Texture on success, or IntPtr.Zero on failure.</returns>
        [DllImport(DllName, EntryPoint = "IMG_LoadTexture", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr LoadTexture(IntPtr renderer, [MarshalAs(UnmanagedType.LPUTF8Str)] string file);
    }
}
