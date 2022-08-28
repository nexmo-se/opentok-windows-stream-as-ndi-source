using OpenTok;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NewTek;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace NDISource
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:NDISource"
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:NDISource;assembly=NDIRenderer"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right-click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:NDIRenderer/>
    ///
    /// </summary>
    public class NDIRenderer : Control, IVideoRenderer
    {
        private int FrameWidth = -1;
        private int FrameHeight = -1;
        private const int SAMPLING_RATE = 44100;
        private const int NUM_BITS = 16;
        private const int NUM_CHANNELS = 1;
        private WriteableBitmap VideoBitmap;
        public IntPtr sendInstancePtr ;
        public bool EnableBlueFilter;
        static NDIRenderer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NDIRenderer), new FrameworkPropertyMetadata(typeof(NDIRenderer)));
        }

        public NDIRenderer(IntPtr sendInstancePtrx)
        {
            sendInstancePtr = sendInstancePtrx;
            var brush = new ImageBrush();
            brush.Stretch = Stretch.UniformToFill;
            Background = brush;
        }

        public NDIRenderer()
        {
            var brush = new ImageBrush();
            brush.Stretch = Stretch.UniformToFill;
            Background = brush;

        }

        public void RenderFrame(OpenTok.VideoFrame frame)
        {


            // WritableBitmap has to be accessed from a STA thread
            Dispatcher.BeginInvoke(new Action(() =>
            {

                try
                {

                    FrameWidth = frame.Width;
                    FrameHeight = frame.Height;
                    VideoBitmap = new WriteableBitmap(FrameWidth, FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                    IntPtr[] vbuffer = { VideoBitmap.BackBuffer };
                    int[] stride = { VideoBitmap.BackBufferStride };
                    frame.ConvertInPlace(OpenTok.PixelFormat.FormatArgb32, vbuffer, stride);

                    NDIlib.video_frame_v2_t ndi_frame_data = new NDIlib.video_frame_v2_t();

                    ndi_frame_data.xres = FrameWidth;
                    ndi_frame_data.yres = FrameHeight;
                    ndi_frame_data.FourCC = NDIlib.FourCC_type_e.FourCC_type_BGRA;
                    ndi_frame_data.p_data = VideoBitmap.BackBuffer;
                    NDIlib.send_send_video_v2(sendInstancePtr, ref ndi_frame_data);

                    if (Background is ImageBrush)
                    {
                        ImageBrush b = (ImageBrush)Background;
                        b.ImageSource = VideoBitmap;
                    }
                    else
                    {
                        throw new Exception("Please use an ImageBrush as background in the SampleVideoRenderer control");
                    }


                    if (VideoBitmap != null)
                    {
                        VideoBitmap.Lock();
                        {

                            frame.ConvertInPlace(OpenTok.PixelFormat.FormatArgb32, vbuffer, stride);
                        }
                        VideoBitmap.AddDirtyRect(new Int32Rect(0, 0, FrameWidth, FrameHeight));
                        VideoBitmap.Unlock();
                    }
                }
                finally
                {
                    frame.Dispose();
                }
            }));
        }
    }
}
