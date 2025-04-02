using System;
using System.Runtime.InteropServices;

namespace OpenCvSharp
{
#pragma warning disable 1591
    // ReSharper disable InconsistentNaming

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionByte(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec2b(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec3b(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec4b(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec6b(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionInt16(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec2s(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec3s(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec4s(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec6s(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionInt32(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec2i(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec3i(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec4i(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec6i(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionFloat(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec2f(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec3f(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec4f(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec6f(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionDouble(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec2d(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec3d(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec4d(IntPtr value, IntPtr position);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MatForeachFunctionVec6d(IntPtr value, IntPtr position);
}
