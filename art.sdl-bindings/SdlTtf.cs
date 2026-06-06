using System;
using System.Runtime.InteropServices;

namespace Art2Core.SdlBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_Color
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public SDL_Color(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_Surface
    {
        public uint flags;
        public uint format;
        public int w;
        public int h;
        public int pitch;
        public IntPtr pixels;
        public int refcount;
        public IntPtr reserved;
    }

    public static partial class SdlTtf
    {
        private const string DllName = "SDL3_ttf";

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool TTF_Init();

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void TTF_Quit();

        [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr TTF_OpenFont(string file, float ptsize);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void TTF_CloseFont(IntPtr font);

        [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr TTF_RenderText_Blended(IntPtr font, string text, nuint length, SDL_Color fg);

        [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool TTF_GetStringSize(IntPtr font, string text, nuint length, out int w, out int h);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int TTF_GetFontHeight(IntPtr font);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int TTF_GetFontAscent(IntPtr font);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int TTF_GetFontDescent(IntPtr font);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool TTF_GetGlyphMetrics(IntPtr font, uint ch, out int minx, out int maxx, out int miny, out int maxy, out int advance);

        [LibraryImport(DllName)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr TTF_RenderGlyph_Blended(IntPtr font, uint ch, SDL_Color fg);
    }
}
