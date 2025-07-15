#if NETCOREAPP3_0_OR_GREATER
#nullable enable
#endif

using System;
using System.Runtime.InteropServices;

using FFmpeg.AutoGen;


namespace RtspFrameGrabber.Utils
{
    public class FFmpegLogger
    {
#if NETCOREAPP3_0_OR_GREATER
        private static av_log_set_callback_callback? _logCallback;
#else
        private static av_log_set_callback_callback _logCallback;
#endif

        /// <summary>
        /// Set FFmpeg log.
        /// </summary>
        /// <param name="logLevel">Log level</param>
        /// <param name="logFlags">Log flags, support &amp; operator.</param>
#if NETCOREAPP3_0_OR_GREATER
        public static unsafe void SetupLogging(LogLevel logLevel = LogLevel.Verbose, LogFlags logFlags = LogFlags.PrintLevel, Action<string, int>? logWrite = null)
#else
        public static unsafe void SetupLogging(LogLevel logLevel = LogLevel.Verbose, LogFlags logFlags = LogFlags.PrintLevel, Action<string, int> logWrite = null)
#endif
        {
            ffmpeg.av_log_set_level((int)logLevel);
            ffmpeg.av_log_set_flags((int)logFlags);

            if (logWrite == null)
            {
                _logCallback = ffmpeg.av_log_default_callback;
            }
            else
            {
                _logCallback = (p0, level, format, vl) =>
                {
                    if (level > ffmpeg.av_log_get_level()) return;
                    var lineSize = 1024;
                    var printPrefix = 1;
                    var lineBuffer = stackalloc byte[lineSize];
                    ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
                    logWrite.Invoke(((IntPtr)lineBuffer).PtrToStringUTF8(), level);
                };
            }

            ffmpeg.av_log_set_callback(_logCallback);
        }
    }


    internal static class IntPtrExtensions
    {
        public static string PtrToStringUTF8(this IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;

            // Busca el final de la cadena (byte nulo)
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
                len++;

            byte[] buffer = new byte[len];
            Marshal.Copy(ptr, buffer, 0, len);
            return System.Text.Encoding.UTF8.GetString(buffer);
        }
    }


    public enum LogLevel : int
    {
        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_MAX_OFFSET"/>
        /// </summary>
        All = ffmpeg.AV_LOG_MAX_OFFSET,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_TRACE"/>
        /// </summary>
        Trace = ffmpeg.AV_LOG_TRACE,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_DEBUG"/>
        /// </summary>
        Debug = ffmpeg.AV_LOG_DEBUG,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_VERBOSE"/>
        /// </summary>
        Verbose = ffmpeg.AV_LOG_VERBOSE,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_WARNING"/>
        /// </summary>
        Warning = ffmpeg.AV_LOG_WARNING,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_ERROR"/>
        /// </summary>
        Error = ffmpeg.AV_LOG_ERROR,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_FATAL"/>
        /// </summary>
        Fatal = ffmpeg.AV_LOG_FATAL,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_PANIC"/>
        /// </summary>
        Panic = ffmpeg.AV_LOG_PANIC,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_QUIET"/>
        /// </summary>
        Quiet = ffmpeg.AV_LOG_QUIET,
    }


    [Flags]
    public enum LogFlags : int
    {
        None = 0,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_SKIP_REPEATED"/>
        /// </summary>
        SkipRepeated = ffmpeg.AV_LOG_SKIP_REPEATED,

        /// <summary>
        /// <see cref="ffmpeg.AV_LOG_PRINT_LEVEL"/>
        /// </summary>
        PrintLevel = ffmpeg.AV_LOG_PRINT_LEVEL,
    }
}
