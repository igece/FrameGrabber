# FrameGrabber

.NET library to extract video frames from a RTSP stream.

The extracted frames can be used for various purposes, such as image processing, computer vision, or machine learning tasks.

Makes use of the FFmpeg libraries and [FFmpeg.AutoGen](shttps://github.com/Ruslan-B/FFmpeg.AutoGen) to handle the underlying FFmpeg functionality.

## Usage

To use the FrameGrabber library, you need to install the NuGet package:

```bash
dotnet add package FrameGrabber
```

Then, you can use the `FrameGrabber` class to extract frames from a RTSP stream:

```csharp
using FrameGrabber;
using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var rtspUrl = "rtsp://your_rtsp_stream_url";
        var frameGrabber = new RtspVideoFrameGrabber();
        frameGrabber.Open(rtspUrl);

        while (var frame = frameGrabber.DecodeNextFrame())
        {
            // Process the frame (e.g., save it, display it, etc.)
   
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb))
            {
                var rect = new Rectangle(0, 0, width, height);
                var bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                System.Runtime.InteropServices.Marshal.Copy(rgb32Data, 0, bmpData.Scan0, rgb32Data.Length);

                bitmap.UnlockBits(bmpData);
                bitmap.Save($"frame_{DateTime.Now:yyyyMMdd_HHmmss}.jpg", ImageFormat.Jpeg);
            }   
        }
    }
}
```
