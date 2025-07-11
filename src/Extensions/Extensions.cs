#if NETCOREAPP3_0_OR_GREATER
#nullable enable
#endif

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using FFmpeg.AutoGen;

using FrameGrabber.Exceptions;


namespace FrameGrabber.Extensions
{
    internal static class Extensions
    {
        public static unsafe AVDictionary* ToAVDictionary(this Dictionary<string, string> dict)
        {
            AVDictionary* avDict = null;

            foreach (var kv in dict)
                ffmpeg.av_dict_set(&avDict, kv.Key, kv.Value, 0);

            return avDict;

        }


        public static int ThrowExceptionIfError(this int error)
        {
            if (error < 0)
                throw new FFmpegException(error, ErrorCodeToString(error));

            return error;
        }


#if NETCOREAPP3_0_OR_GREATER
        private static unsafe string? ErrorCodeToString(int error)
#else
        private static unsafe string ErrorCodeToString(int error)
#endif
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
#if NETCOREAPP3_0_OR_GREATER
            var message = Marshal.PtrToStringAnsi((nint)buffer);
#else
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
#endif

            return message;
        }
    }
}
