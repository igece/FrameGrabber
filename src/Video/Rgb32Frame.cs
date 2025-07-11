using System;


namespace FrameGrabber.Video
{
    public class Rgb32Frame
    {
        public byte[] Data { get; }

        public int Width { get; }

        public int Height { get; }


        public Rgb32Frame(byte[] data, int width, int height)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Width = width > 0 ? width : throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero");
            Height = height > 0 ? height : throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero");
        }
    }
}
