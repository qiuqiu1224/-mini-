using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFmpeg.AutoGen;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace Test
{
    public unsafe class tstRtmp
    {
        public tstRtmp()
        {
            
        }
        /// <summary>
        /// 显示图片委托
        /// </summary>
        /// <param name="bitmap"></param>
        public delegate void ShowBitmap(Bitmap bitmap);
        /// <summary>
        /// 执行控制变量
        /// </summary>
        bool CanRun;

        /// <summary>
        /// 对读取的264数据包进行解码和转换
        /// </summary>
        /// <param name="show">解码完成回调函数</param>
        /// <param name="url">播放地址，也可以是本地文件地址</param>
        [Obsolete]
        public unsafe void Start(ShowBitmap show, string url)
        {
            CanRun = true;

            Console.WriteLine(@"Current directory: " + Environment.CurrentDirectory);
            Console.WriteLine(@"Runnung in {0}-bit mode.", Environment.Is64BitProcess ? @"64" : @"32");
            //FFmpegDLL目录查找和设置
            FFmpegBinariesHelper.RegisterFFmpegBinaries();

            #region ffmpeg 初始化
            // 初始化注册ffmpeg相关的编码器
            ffmpeg.av_register_all();
            ffmpeg.avcodec_register_all();
            ffmpeg.avformat_network_init();
            #endregion

            #region ffmpeg 日志
            // 设置记录ffmpeg日志级别
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);
            av_log_set_callback_callback logCallback = (p0, level, format, vl) =>
            {
                if (level > ffmpeg.av_log_get_level()) return;

                var lineSize = 1024;
                var lineBuffer = stackalloc byte[lineSize];
                var printPrefix = 1;
                ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
                var line = Marshal.PtrToStringAnsi((IntPtr)lineBuffer);
                Console.Write(line);
            };
            ffmpeg.av_log_set_callback(logCallback);

            #endregion
            #region ffmpeg 转码


            // 分配音视频格式上下文
            AVFormatContext* pFormatContext = ffmpeg.avformat_alloc_context();

            int error;
            //设置参数
            //设置TCP方式拉流
            AVDictionary* format_opts = null;
            ffmpeg.av_dict_set(&format_opts, "stimeout", Convert.ToString(2 * 1000000), 0); //设置链接超时时间（us）
            ffmpeg.av_dict_set(&format_opts, "rtsp_transport", "tcp", 0); //设置推流的方式，默认udp。
            ffmpeg.av_dict_set(&format_opts, "max_delay", "500000", 0); //设置最大时延
            ffmpeg.av_dict_set(&format_opts, "buffer_size", "204800", 0); //设置缓存大小，1080p可将值调大

            //打开流
            //error = ffmpeg.avformat_open_input(&pFormatContext, url, null, null);
            error = ffmpeg.avformat_open_input(&pFormatContext, url, null, &format_opts);
            if (error != 0)
            {
                throw new ApplicationException(GetErrorMessage(error));
            }


            // 读取媒体流信息
            error = ffmpeg.avformat_find_stream_info(pFormatContext, null);
            if (error != 0) throw new ApplicationException(GetErrorMessage(error));

            // 这里只是为了打印些视频参数
            AVDictionaryEntry* tag = null;
            while ((tag = ffmpeg.av_dict_get(pFormatContext->metadata, "", tag, ffmpeg.AV_DICT_IGNORE_SUFFIX)) != null)
            {
                var key = Marshal.PtrToStringAnsi((IntPtr)tag->key);
                var value = Marshal.PtrToStringAnsi((IntPtr)tag->value);
                Console.WriteLine($"{key} = {value}");
            }

            // 从格式化上下文获取流索引
            AVStream* pStream = null, aStream;
            for (var i = 0; i < pFormatContext->nb_streams; i++)
            {
                if (pFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    pStream = pFormatContext->streams[i];

                }
                else if (pFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    aStream = pFormatContext->streams[i];

                }
            }
            if (pStream == null) throw new ApplicationException(@"Could not found video stream.");



            // 获取流的编码器上下文
            var codecContext = *pStream->codec;

            Console.WriteLine($"codec name: {ffmpeg.avcodec_get_name(codecContext.codec_id)}");
            // 获取图像的宽、高及像素格式
            var width = codecContext.width;
            var height = codecContext.height;
            var sourcePixFmt = codecContext.pix_fmt;

            // 得到编码器ID
            var codecId = codecContext.codec_id;
            // 目标像素格式
            var destinationPixFmt = AVPixelFormat.AV_PIX_FMT_BGR24;


            // 某些264格式codecContext.pix_fmt获取到的格式是AV_PIX_FMT_NONE 统一都认为是YUV420P
            if (sourcePixFmt == AVPixelFormat.AV_PIX_FMT_NONE && codecId == AVCodecID.AV_CODEC_ID_H264)
            {
                sourcePixFmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            }

            // 得到SwsContext对象：用于图像的缩放和转换操作
            var pConvertContext = ffmpeg.sws_getContext(width, height, sourcePixFmt,
                width, height, destinationPixFmt,
                ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            if (pConvertContext == null) throw new ApplicationException(@"Could not initialize the conversion context.");

            //分配一个默认的帧对象:AVFrame
            var pConvertedFrame = ffmpeg.av_frame_alloc();
            // 目标媒体格式需要的字节长度
            var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(destinationPixFmt, width, height, 1);
            // 分配目标媒体格式内存使用
            var convertedFrameBufferPtr = Marshal.AllocHGlobal(convertedFrameBufferSize);
            var dstData = new byte_ptrArray4();
            var dstLinesize = new int_array4();
            // 设置图像填充参数
            ffmpeg.av_image_fill_arrays(ref dstData, ref dstLinesize, (byte*)convertedFrameBufferPtr, destinationPixFmt, width, height, 1);

            #endregion

            #region ffmpeg 解码
            // 根据编码器ID获取对应的解码器
            var pCodec = ffmpeg.avcodec_find_decoder(codecId);
            if (pCodec == null) throw new ApplicationException(@"Unsupported codec.");

            var pCodecContext = &codecContext;

            if ((pCodec->capabilities & ffmpeg.AV_CODEC_CAP_TRUNCATED) == ffmpeg.AV_CODEC_CAP_TRUNCATED)
                pCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_TRUNCATED;

            // 通过解码器打开解码器上下文:AVCodecContext pCodecContext
            error = ffmpeg.avcodec_open2(pCodecContext, pCodec, null);
            if (error < 0) throw new ApplicationException(GetErrorMessage(error));

            // 分配解码帧对象：AVFrame pDecodedFrame
            var pDecodedFrame = ffmpeg.av_frame_alloc();

            // 初始化媒体数据包
            var packet = new AVPacket();
            var pPacket = &packet;
            ffmpeg.av_init_packet(pPacket);

            var frameNumber = 0;
            while (CanRun)
            {
                try
                {

                    do
                    {
                        // 读取一帧未解码数据
                        error = ffmpeg.av_read_frame(pFormatContext, pPacket);
                        // Console.WriteLine(pPacket->dts);
                        if (error == ffmpeg.AVERROR_EOF)
                        {
                            Console.WriteLine("error == ffmpeg.AVERROR_EOF");
                            break;
                        }
                        if (error < 0)
                        {
                            string str;
                            str = "error < 0" + GetErrorMessage(error);
                            throw new ApplicationException(str);
                        }

                        if (pPacket->stream_index != pStream->index) continue;

                        // 解码
                        error = ffmpeg.avcodec_send_packet(pCodecContext, pPacket);
                        if (error < 0) throw new ApplicationException(GetErrorMessage(error));
                        // 解码输出解码数据
                        error = ffmpeg.avcodec_receive_frame(pCodecContext, pDecodedFrame);
                    } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN) && CanRun);
                    if (error == ffmpeg.AVERROR_EOF)
                    {
                        break;
                    }
                    if (error < 0) throw new ApplicationException(GetErrorMessage(error));

                    if (pPacket->stream_index != pStream->index) continue;

                    //Console.WriteLine($@"frame: {frameNumber}");
                    // YUV->RGB
                    ffmpeg.sws_scale(pConvertContext, pDecodedFrame->data, pDecodedFrame->linesize, 0, height, dstData, dstLinesize);
                }
                finally
                {
                    ffmpeg.av_packet_unref(pPacket);//释放数据包对象引用
                    ffmpeg.av_frame_unref(pDecodedFrame);//释放解码帧对象引用
                }

                // 封装Bitmap图片
                //添加名称时间
                //Graphics g;
                var bitmap = new Bitmap(width, height, dstLinesize[0], PixelFormat.Format24bppRgb, convertedFrameBufferPtr);

                //g = Graphics.FromImage(bitmap);
                //string time= DateTime.Now.ToLocalTime().ToString();
                //string strmsg = devicesname;
                //Font fromat = new Font("宋体", 20);
                //StringFormat format = new StringFormat();
                //SizeF size = g.MeasureString(strmsg, fromat);   //   MeasureText(strmsg, new Font("宋体", 30));
                //g.DrawString(strmsg, fromat, System.Drawing.Brushes.White, 0, 40);

                //g.DrawString(time, fromat, System.Drawing.Brushes.White, 0, 0);

                // 回调
                show(bitmap);
                // bitmap.Save(AppDomain.CurrentDomain.BaseDirectory + "\\264\\frame.buffer."+ frameNumber + ".jpg", ImageFormat.Jpeg);

                frameNumber++;
            }
            //播放完置空播放图片 
            show(null);

            #endregion

            #region 释放资源
            Marshal.FreeHGlobal(convertedFrameBufferPtr);
            ffmpeg.av_free(pConvertedFrame);
            ffmpeg.sws_freeContext(pConvertContext);

            ffmpeg.av_free(pDecodedFrame);
            ffmpeg.avcodec_close(pCodecContext);
            ffmpeg.avformat_close_input(&pFormatContext);


            #endregion
        }

        public unsafe void Start_save(ShowBitmap show, string url, string filename)
        {
            CanRun = true;
            CanRunlz = true;
            Console.WriteLine(@"Current directory: " + Environment.CurrentDirectory);
            Console.WriteLine(@"Runnung in {0}-bit mode.", Environment.Is64BitProcess ? @"64" : @"32");
            //FFmpegDLL目录查找和设置
            FFmpegBinariesHelper.RegisterFFmpegBinaries();

            #region ffmpeg 初始化
            // 初始化注册ffmpeg相关的编码器
            ffmpeg.av_register_all();
            ffmpeg.avcodec_register_all();
            ffmpeg.avformat_network_init();
            #endregion

            #region ffmpeg 日志
            // 设置记录ffmpeg日志级别
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);
            av_log_set_callback_callback logCallback = (p0, level, format, vl) =>
            {
                if (level > ffmpeg.av_log_get_level()) return;

                var lineSize = 1024;
                var lineBuffer = stackalloc byte[lineSize];
                var printPrefix = 1;
                ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
                var line = Marshal.PtrToStringAnsi((IntPtr)lineBuffer);
                Console.Write(line);
            };
            ffmpeg.av_log_set_callback(logCallback);

            #endregion

            #region ffmpeg 转码


            // 分配音视频格式上下文
            AVFormatContext* pFormatContext = ffmpeg.avformat_alloc_context();

            int error;

            //打开流
            error = ffmpeg.avformat_open_input(&pFormatContext, url, null, null);
            if (error != 0) throw new ApplicationException(GetErrorMessage(error));

            // 读取媒体流信息
            error = ffmpeg.avformat_find_stream_info(pFormatContext, null);
            if (error != 0) throw new ApplicationException(GetErrorMessage(error));

            // 这里只是为了打印些视频参数
            AVDictionaryEntry* tag = null;
            while ((tag = ffmpeg.av_dict_get(pFormatContext->metadata, "", tag, ffmpeg.AV_DICT_IGNORE_SUFFIX)) != null)
            {
                var key = Marshal.PtrToStringAnsi((IntPtr)tag->key);
                var value = Marshal.PtrToStringAnsi((IntPtr)tag->value);
                Console.WriteLine($"{key} = {value}");
            }

            // 从格式化上下文获取流索引
            AVStream* pStream = null, aStream;
            for (var i = 0; i < pFormatContext->nb_streams; i++)
            {
                if (pFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    pStream = pFormatContext->streams[i];

                }
                else if (pFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    aStream = pFormatContext->streams[i];

                }
            }
            if (pStream == null) throw new ApplicationException(@"Could not found video stream.");

            // 获取流的编码器上下文
            var codecContext = *pStream->codec;

            Console.WriteLine($"codec name: {ffmpeg.avcodec_get_name(codecContext.codec_id)}");
            // 获取图像的宽、高及像素格式
            var width = codecContext.width;
            var height = codecContext.height;
            var sourcePixFmt = codecContext.pix_fmt;

            // 得到编码器ID
            var codecId = codecContext.codec_id;
            // 目标像素格式
            var destinationPixFmt = AVPixelFormat.AV_PIX_FMT_BGR24;


            // 某些264格式codecContext.pix_fmt获取到的格式是AV_PIX_FMT_NONE 统一都认为是YUV420P
            if (sourcePixFmt == AVPixelFormat.AV_PIX_FMT_NONE && codecId == AVCodecID.AV_CODEC_ID_H264)
            {
                sourcePixFmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            }

            // 得到SwsContext对象：用于图像的缩放和转换操作
            var pConvertContext = ffmpeg.sws_getContext(width, height, sourcePixFmt,
                width, height, destinationPixFmt,
                ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            if (pConvertContext == null) throw new ApplicationException(@"Could not initialize the conversion context.");

            //分配一个默认的帧对象:AVFrame
            var pConvertedFrame = ffmpeg.av_frame_alloc();
            // 目标媒体格式需要的字节长度
            var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(destinationPixFmt, width, height, 1);
            // 分配目标媒体格式内存使用
            var convertedFrameBufferPtr = Marshal.AllocHGlobal(convertedFrameBufferSize);
            var dstData = new byte_ptrArray4();
            var dstLinesize = new int_array4();
            // 设置图像填充参数
            ffmpeg.av_image_fill_arrays(ref dstData, ref dstLinesize, (byte*)convertedFrameBufferPtr, destinationPixFmt, width, height, 1);

            #endregion

            #region ffmpeg 解码
            // 根据编码器ID获取对应的解码器
            var pCodec = ffmpeg.avcodec_find_decoder(codecId);
            if (pCodec == null) throw new ApplicationException(@"Unsupported codec.");

            var pCodecContext = &codecContext;

            if ((pCodec->capabilities & ffmpeg.AV_CODEC_CAP_TRUNCATED) == ffmpeg.AV_CODEC_CAP_TRUNCATED)
                pCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_TRUNCATED;

            // 通过解码器打开解码器上下文:AVCodecContext pCodecContext
            error = ffmpeg.avcodec_open2(pCodecContext, pCodec, null);
            if (error < 0) throw new ApplicationException(GetErrorMessage(error));

            // 分配解码帧对象：AVFrame pDecodedFrame
            var pDecodedFrame = ffmpeg.av_frame_alloc();

            // 初始化媒体数据包
            var packet = new AVPacket();
            var pPacket = &packet;
            ffmpeg.av_init_packet(pPacket);

            var frameNumber = 0;
            //
            AVFormatContext* o_fmt_ctx;
            AVStream* o_video_stream;
            o_fmt_ctx = null;
            if (st)
            {
                ffmpeg.avformat_alloc_output_context2(&o_fmt_ctx, null, null, filename);

                /*
                * since all input files are supposed to be identical (framerate, dimension, color format, ...)
                * we can safely set output codec values from first input file
                */
                o_video_stream = ffmpeg.avformat_new_stream(o_fmt_ctx, null);
                {
                    AVCodecContext* c;
                    c = o_video_stream->codec;
                    c->bit_rate = 400000;
                    c->codec_id = pStream->codec->codec_id;
                    c->codec_type = pStream->codec->codec_type;
                    c->time_base.num = pStream->time_base.num;
                    c->time_base.den = pStream->time_base.den;
                    //fprintf(stderr, "time_base.num = %d time_base.den = %d\n", c->time_base.num, c->time_base.den);
                    c->width = pStream->codec->width;
                    c->height = pStream->codec->height;
                    c->pix_fmt = pStream->codec->pix_fmt;
                    //printf("%d %d %d", c->width, c->height, c->pix_fmt);
                    c->flags = pStream->codec->flags;
                    c->flags |= ffmpeg.CODEC_FLAG_GLOBAL_HEADER;
                    c->me_range = pStream->codec->me_range;
                    c->max_qdiff = pStream->codec->max_qdiff;
                    c->qmin = pStream->codec->qmin;
                    c->qmax = pStream->codec->qmax;
                    c->qcompress = pStream->codec->qcompress;
                }

                ffmpeg.avio_open(&o_fmt_ctx->pb, filename, ffmpeg.AVIO_FLAG_WRITE);
                ffmpeg.avformat_write_header(o_fmt_ctx, null);
            }
            long last_pts = 0;
            long last_dts = 0;
            long pts = 0;
            long dts = 0;

            while (CanRun)
            {
                try
                {
                    do
                    {
                        // 读取一帧未解码数据
                        error = ffmpeg.av_read_frame(pFormatContext, pPacket);
                        Console.WriteLine(pPacket->dts);
                        if (error == ffmpeg.AVERROR_EOF) break;
                        if (error < 0) throw new ApplicationException(GetErrorMessage(error));

                        if (pPacket->stream_index != pStream->index) continue;

                        //添加录制
                        if (st)
                        {

                            pPacket->flags |= ffmpeg.AV_PKT_FLAG_KEY;
                            pts = pPacket->pts;
                            pPacket->pts += last_pts;
                            dts = pPacket->dts;
                            pPacket->dts += last_dts;
                            pPacket->stream_index = 0;

                            ffmpeg.av_interleaved_write_frame(o_fmt_ctx, pPacket);
                        }
                        //
                        // 解码
                        error = ffmpeg.avcodec_send_packet(pCodecContext, pPacket);
                        if (error < 0) throw new ApplicationException(GetErrorMessage(error));
                        // 解码输出解码数据
                        error = ffmpeg.avcodec_receive_frame(pCodecContext, pDecodedFrame);
                    } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN) && CanRun);
                    if (error == ffmpeg.AVERROR_EOF) break;
                    if (error < 0) throw new ApplicationException(GetErrorMessage(error));

                    if (pPacket->stream_index != pStream->index) continue;

                    Console.WriteLine($@"frame: {frameNumber}");
                    // YUV->RGB
                    ffmpeg.sws_scale(pConvertContext, pDecodedFrame->data, pDecodedFrame->linesize, 0, height, dstData, dstLinesize);
                    //录制结束
                    if (!CanRunlz)
                    {
                        last_dts += dts;
                        last_pts += pts;
                        ffmpeg.av_write_trailer(o_fmt_ctx);
                        ffmpeg.avcodec_close(o_fmt_ctx->streams[0]->codec);
                        ffmpeg.av_freep(&o_fmt_ctx->streams[0]->codec);
                        ffmpeg.av_freep(&o_fmt_ctx->streams[0]);
                        ffmpeg.avio_close(o_fmt_ctx->pb);
                        ffmpeg.av_free(o_fmt_ctx);
                    }
                }
                finally
                {
                    ffmpeg.av_packet_unref(pPacket);//释放数据包对象引用
                    ffmpeg.av_frame_unref(pDecodedFrame);//释放解码帧对象引用
                }

                // 封装Bitmap图片
                var bitmap = new Bitmap(width, height, dstLinesize[0], PixelFormat.Format24bppRgb, convertedFrameBufferPtr);
                // 回调
                show(bitmap);
                //bitmap.Save(AppDomain.CurrentDomain.BaseDirectory + "\\264\\frame.buffer."+ frameNumber + ".jpg", ImageFormat.Jpeg);

                frameNumber++;
            }
            //播放完置空播放图片 
            show(null);

            #endregion

            #region 释放资源
            Marshal.FreeHGlobal(convertedFrameBufferPtr);
            ffmpeg.av_free(pConvertedFrame);
            ffmpeg.sws_freeContext(pConvertContext);

            ffmpeg.av_free(pDecodedFrame);
            ffmpeg.avcodec_close(pCodecContext);
            ffmpeg.avformat_close_input(&pFormatContext);


            #endregion
        }

        /// <summary>
        /// 获取ffmpeg错误信息
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private static unsafe string GetErrorMessage(int error)
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
            return message;
        }
        /// 执行控制变量
        /// </summary>
        bool CanRunlz;
        bool st = false;
        /// <summary>
        /// 对读取的264数据包进行解码和转换
        /// </summary>
        /// <param name="url">播放地址，也可以是本地文件地址</param>
        public unsafe void StartSave(ShowBitmap show, string url, string filename)
        {
            try
            {
                CanRunlz = true;
                


                AVFormatContext* i_fmt_ctx;
                AVStream* i_video_stream = null;
                AVFormatContext* o_fmt_ctx;
                AVStream* o_video_stream;

                /* should set to null so that avformat_open_input() allocate a new one */
                i_fmt_ctx = null;
                if (ffmpeg.avformat_open_input(&i_fmt_ctx, url, null, null) != 0)
                {
                    return;
                }

                if (ffmpeg.avformat_find_stream_info(i_fmt_ctx, null) < 0)
                {
                    return;
                }

                //av_dump_format(i_fmt_ctx, 0, argv[1], 0);

                /* find first video stream */
                for (uint i = 0; i < i_fmt_ctx->nb_streams; i++)
                {
                    if (i_fmt_ctx->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                    {
                        i_video_stream = i_fmt_ctx->streams[i];
                        break;
                    }
                }

                ffmpeg.avformat_alloc_output_context2(&o_fmt_ctx, null, null, filename);

                /*
                * since all input files are supposed to be identical (framerate, dimension, color format, ...)
                * we can safely set output codec values from first input file
                */
                o_video_stream = ffmpeg.avformat_new_stream(o_fmt_ctx, null);
                {
                    AVCodecContext* c;
                    c = o_video_stream->codec;
                    c->bit_rate = 400000;
                    c->codec_id = i_video_stream->codec->codec_id;
                    c->codec_type = i_video_stream->codec->codec_type;
                    c->time_base.num = i_video_stream->time_base.num;
                    c->time_base.den = i_video_stream->time_base.den;
                    //fprintf(stderr, "time_base.num = %d time_base.den = %d\n", c->time_base.num, c->time_base.den);
                    c->width = i_video_stream->codec->width;
                    c->height = i_video_stream->codec->height;
                    c->pix_fmt = i_video_stream->codec->pix_fmt;
                    //printf("%d %d %d", c->width, c->height, c->pix_fmt);
                    c->flags = i_video_stream->codec->flags;
                    c->flags |= ffmpeg.CODEC_FLAG_GLOBAL_HEADER;
                    c->me_range = i_video_stream->codec->me_range;
                    c->max_qdiff = i_video_stream->codec->max_qdiff;
                    c->qmin = i_video_stream->codec->qmin;
                    c->qmax = i_video_stream->codec->qmax;
                    c->qcompress = i_video_stream->codec->qcompress;
                }

                ffmpeg.avio_open(&o_fmt_ctx->pb, filename, ffmpeg.AVIO_FLAG_WRITE);
                ffmpeg.avformat_write_header(o_fmt_ctx, null);

                long last_pts = 0;
                long last_dts = 0;
                long pts = 0;
                long dts = 0;
                while (CanRunlz)
                {
                    AVPacket i_pkt;
                    ffmpeg.av_init_packet(&i_pkt);
                    i_pkt.size = 0;
                    i_pkt.data = null;
                    if (ffmpeg.av_read_frame(i_fmt_ctx, &i_pkt) < 0)
                        break;
                    /*
                    * pts and dts should increase monotonically
                    * pts should be >= dts
                    */
                    i_pkt.flags |= ffmpeg.AV_PKT_FLAG_KEY;
                    pts = i_pkt.pts;
                    i_pkt.pts += last_pts;
                    dts = i_pkt.dts;
                    i_pkt.dts += last_dts;
                    i_pkt.stream_index = 0;

                    //printf("%lld %lld\n", i_pkt.pts, i_pkt.dts);
                    int num = 1;
                    //printf("frame %d\n", num++);
                    ffmpeg.av_interleaved_write_frame(o_fmt_ctx, &i_pkt);
                    //av_free_packet(&i_pkt);
                    //av_init_packet(&i_pkt);
                }
                last_dts += dts;
                last_pts += pts;
                ffmpeg.avformat_close_input(&i_fmt_ctx);
                ffmpeg.av_write_trailer(o_fmt_ctx);
                ffmpeg.avcodec_close(o_fmt_ctx->streams[0]->codec);
                ffmpeg.av_freep(&o_fmt_ctx->streams[0]->codec);
                ffmpeg.av_freep(&o_fmt_ctx->streams[0]);
                ffmpeg.avio_close(o_fmt_ctx->pb);
                ffmpeg.av_free(o_fmt_ctx);
            }
            catch (Exception ex)
            {
            }
        }
        
        public void Startlz()
        {
            st = true;
        }
        public void Stoplz()
        {
            st = false;
            CanRunlz = false;
            
        }
        public void Stop()
        {
            CanRun = false;
        }

    }
}
