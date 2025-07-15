#if NETCOREAPP3_0_OR_GREATER
#nullable enable
#endif

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

using FFmpeg.AutoGen;

using RtspFrameGrabber.Exceptions;
using RtspFrameGrabber.Extensions;


namespace RtspFrameGrabber.Video
{
    public sealed unsafe class RtspVideoFrameGrabber : IDisposable
    {
        private AVCodecContext* _codecContext;
        private AVFormatContext* _formatContext;
        private SwsContext* _swsContext;

        private AVPacket* _packet;
        private AVFrame* _hwFrame;
        private AVFrame* _frame;

        private int _streamIndex;

#if NETCOREAPP3_0_OR_GREATER
        private readonly Action<string>? _logCallback;
#else
        private readonly Action<string> _logCallback;
#endif


        public bool IsOpen { get; private set; }

#if NETCOREAPP3_0_OR_GREATER
        public string? CodecName { get; private set; }
#else
        public string CodecName { get; private set; }
#endif

#if NETCOREAPP3_0_OR_GREATER
        public Size? FrameSize { get; private set; }
# else
        public Size FrameSize { get; private set; }
#endif

#if NETCOREAPP3_0_OR_GREATER
        public AVPixelFormat? PixelFormat { get; private set; }
#else
        public AVPixelFormat PixelFormat { get; private set; }
#endif


#if NETCOREAPP3_0_OR_GREATER
        public RtspVideoFrameGrabber(Action<string>? logCallback = null)
#else
        public RtspVideoFrameGrabber(Action<string> logCallback = null)
#endif
        {
            _logCallback = logCallback;
            IsOpen = false;
        }

#if NETCOREAPP3_0_OR_GREATER
        public void Open(string url, Dictionary<string, string>? options = null, string? hwAccelCodec = null)
#else
        public void Open(string url, Dictionary<string, string> options = null, string hwAccelCodec = null)
#endif
        {
            if (IsOpen)
                throw new InvalidOperationException("Stream is already open");

            _formatContext = null;
            _codecContext = null;
            _swsContext = null;
            _packet = null;
            _frame = null;
            _hwFrame = null;

            AVDictionary* opts = null;

            try
            {
                _formatContext = ffmpeg.avformat_alloc_context();

                fixed (AVFormatContext** formatContext = &_formatContext)
                {
                    if (options != null && options.Count > 0)
                    {
                        opts = options.ToAVDictionary();
                        ffmpeg.avformat_open_input(formatContext, url, null, &opts).ThrowExceptionIfError();
                    }
                    else
                    {
                        ffmpeg.avformat_open_input(formatContext, url, null, null).ThrowExceptionIfError();
                    }
                }

                ffmpeg.avformat_find_stream_info(_formatContext, null).ThrowExceptionIfError();

                AVCodec* codec = null;

                _streamIndex = ffmpeg
                    .av_find_best_stream(_formatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0)
                    .ThrowExceptionIfError();

                AVHWDeviceType hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;

                if (!string.IsNullOrWhiteSpace(hwAccelCodec))
                {
                    codec = ffmpeg.avcodec_find_decoder_by_name(hwAccelCodec);
                    
                    if (codec == null || codec->type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                        throw new FFmpegException($"Hardware accelerated codec '{hwAccelCodec}' not found");

#if NETCOREAPP3_0_OR_GREATER

                    hwDeviceType = hwAccelCodec[hwAccelCodec.LastIndexOf('_')..] switch
                    {
                        "_venc" or "_cuvid" or "_nvdec" => AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA,
                        "_qsv" => AVHWDeviceType.AV_HWDEVICE_TYPE_QSV,
                        "_vdpau" => AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU,
                        "_vaapi" => AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI,
                        "_d3d11va" or "_d3d11" => AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA,
                        "_videotoolbox" => AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX,
                        "_drm" or "_v4l2m2m" => AVHWDeviceType.AV_HWDEVICE_TYPE_DRM,
                        "_opencl" => AVHWDeviceType.AV_HWDEVICE_TYPE_OPENCL,
                        _ => AVHWDeviceType.AV_HWDEVICE_TYPE_NONE
                    };
#else
                    switch (hwAccelCodec.Substring(hwAccelCodec.LastIndexOf('_')))
                    {
                        case "_venc":
                        case "_cuvid":
                        case "_nvdec":
                            hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA;
                            break;

                        case "_qsv":
                            hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_QSV;
                            break;

                        case "_vdpau":
                            hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU;
                            break;

                        case "_vaapi":
                            hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI;
                            break;

                        case "_d3d11va":
                        case "_d3d11":
                            hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA;
                            break;

                        case "_videotoolbox":
                            hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX;
                            break;

                        case "_drm":
                        case "_v4l2m2m":
                            hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_DRM;
                            break;

                        case "_opencl":
                            hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_OPENCL;
                            break;

                        default:
                            hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
                            break;
                    }

#endif

                    if (hwDeviceType == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                        _logCallback?.Invoke($"Unsupported hardware accelerated codec '{hwAccelCodec}'");
                }

                _codecContext = ffmpeg.avcodec_alloc_context3(codec);

                ffmpeg.avcodec_parameters_to_context(_codecContext, _formatContext->streams[_streamIndex]->codecpar)
                    .ThrowExceptionIfError();

                if (hwDeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE && hwAccelCodec?.EndsWith("_v4l2m2m") == false)
                {
                    ffmpeg.av_hwdevice_ctx_create(&_codecContext->hw_device_ctx, hwDeviceType, null, null, 0)
                        .ThrowExceptionIfError();
                }

                ffmpeg.avcodec_open2(_codecContext, codec, null).ThrowExceptionIfError();
               
                if (_codecContext->pix_fmt == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    _codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    _logCallback?.Invoke($"Cannot detect stream's pixel format, assuming {AVPixelFormat.AV_PIX_FMT_YUV420P}");
                }

                CodecName = ffmpeg.avcodec_get_name(codec->id);
                FrameSize = new Size(_codecContext->width, _codecContext->height);
                PixelFormat = _codecContext->pix_fmt;

#if NETCOREAPP3_0_OR_GREATER
                _logCallback?.Invoke($"Video stream opened successfully (Codec: {CodecName}, Frame size: {FrameSize?.Width}x{FrameSize?.Height}, Pixel format: {PixelFormat})");
#else
                _logCallback?.Invoke($"Video stream opened successfully (Codec: {CodecName}, Frame size: {FrameSize.Width}x{FrameSize.Height}, Pixel format: {PixelFormat})");
#endif

                _packet = ffmpeg.av_packet_alloc();
                _frame = ffmpeg.av_frame_alloc();
                _hwFrame = ffmpeg.av_frame_alloc();

                IsOpen = true;
            }

            catch (FFmpegException)
            {
                Cleanup();
                throw;
            }

            finally
            {
                if (opts != null)
                    ffmpeg.av_dict_free(&opts);
            }
        }


        public void Close()
        {
            if (!IsOpen)
                throw new InvalidOperationException("Stream is not open");

            Cleanup();
            IsOpen = false;
        }


        public void Dispose()
        {
            if (IsOpen)
                Close();
        }


#if NETCOREAPP3_0_OR_GREATER
        public Rgb32Frame? DecodeNextFrame()
#else
        public Rgb32Frame DecodeNextFrame()
#endif
        {
            if (!IsOpen)
                throw new InvalidOperationException("Stream is not open");

            if (_frame != null && _frame != (AVFrame*)0)
                ffmpeg.av_frame_unref(_frame);

            if (_hwFrame != null && _hwFrame != (AVFrame*)0)
                ffmpeg.av_frame_unref(_hwFrame);

            AVFrame frame;
            int error;

            try
            {
                do
                {
                    try
                    {
                        do
                        {
                            ffmpeg.av_packet_unref(_packet);
                            error = ffmpeg.av_read_frame(_formatContext, _packet);

                            if (error == ffmpeg.AVERROR_EOF)
                            {
                                frame = *_frame;
                                return null;
                            }

                            error.ThrowExceptionIfError();
                        } while (_packet->stream_index != _streamIndex);

                        ffmpeg.avcodec_send_packet(_codecContext, _packet).ThrowExceptionIfError();
                    }

                    finally
                    {
                        ffmpeg.av_packet_unref(_packet);
                    }

                    error = ffmpeg.avcodec_receive_frame(_codecContext, _frame);
                } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));

                error.ThrowExceptionIfError();

                if (_codecContext->hw_device_ctx != null)
                {
                    ffmpeg.av_hwframe_transfer_data(_hwFrame, _frame, 0).ThrowExceptionIfError();
                    frame = *_hwFrame;
                }
                else
                    frame = *_frame;

                if (_swsContext == null)
                {
                    _swsContext = ffmpeg.sws_getContext(
                        frame.width,
                        frame.height,
                        (AVPixelFormat)frame.format,                        
                        frame.width,
                        frame.height,
                        AVPixelFormat.AV_PIX_FMT_RGB24,
                        ffmpeg.SWS_POINT, // Not really scaling, just format conversion.
                        null,
                        null,
                        null);

                    if (_swsContext == null)
                        throw new FFmpegException("Unable to obtain a valid conversion context");
                }

                if (_codecContext->pix_fmt == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    _codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    _logCallback?.Invoke($"Cannot detect stream's pixel format, assuming {AVPixelFormat.AV_PIX_FMT_YUV420P}");
                }

                AVFrame* rgbFrame = ffmpeg.av_frame_alloc();

                rgbFrame->width = frame.width;
                rgbFrame->height = frame.height;
                rgbFrame->format = (int)AVPixelFormat.AV_PIX_FMT_RGB24;
                ffmpeg.av_frame_get_buffer(rgbFrame, 32).ThrowExceptionIfError();

                ffmpeg.sws_scale(_swsContext, frame.data, frame.linesize, 0, frame.height,
                    rgbFrame->data, rgbFrame->linesize);
                
                int stride = rgbFrame->linesize[0];
                byte[] data = new byte[rgbFrame->height * stride];

#if NETCOREAPP3_0_OR_GREATER
                Marshal.Copy((nint)rgbFrame->data[0], data, 0, data.Length);
#else
                Marshal.Copy((IntPtr)rgbFrame->data[0], data, 0, data.Length);
#endif

                var managedRgbFrame = new Rgb32Frame(data, rgbFrame->width, rgbFrame->height);
                ffmpeg.av_frame_free(&rgbFrame);

                return managedRgbFrame;
            }

            catch (FFmpegException)
            {
                Close();
                throw;
            }
        }


        private void Cleanup()
        {
            if (_hwFrame != null)
            {
                var hwFrame = _hwFrame;
                ffmpeg.av_frame_free(&hwFrame);
                _hwFrame = null;
            }

            if (_frame != null)
            {
                var frame = _frame;
                ffmpeg.av_frame_free(&frame);
                _frame = null;
            }

            if (_packet != null)
            {
                var packet = _packet;
                ffmpeg.av_packet_free(&packet);
                _packet = null;
            }

            if (_swsContext != null)
            {
                ffmpeg.sws_freeContext(_swsContext);
                _swsContext = null;
            }

            // avcodec_close must be called before avcodec_free_context.
            if (_codecContext != null)
            {
                ffmpeg.avcodec_close(_codecContext);
                var codecContext = _codecContext;
                ffmpeg.avcodec_free_context(&codecContext);
                _codecContext = null;
            }

            if (_formatContext != null)
            {
                var formatContext = _formatContext;
                ffmpeg.avformat_close_input(&formatContext);
                _formatContext = null;
            }
        }
    }
}
