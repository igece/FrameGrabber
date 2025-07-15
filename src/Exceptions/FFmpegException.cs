using System;


namespace RtspFrameGrabber.Exceptions
{
    public class FFmpegException : Exception
    {
        public int? ErrorCode { get; }

        public FFmpegException()
            : base()
        {
        }


        public FFmpegException(string message)
            : base(message)
        {
        }


        public FFmpegException(int errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }


        public FFmpegException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
