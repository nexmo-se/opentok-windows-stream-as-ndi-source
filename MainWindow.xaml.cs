﻿using NewTek;
using NewTek.NDI;
using OpenTok;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Runtime.InteropServices;


namespace NDISource
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string API_KEY = "47464991";
        public const string SESSION_ID = "1_MX40NzQ2NDk5MX5-MTY2MTcwMDA3MDI5N35DT1hTMmFySEVWSnJGckJRelhQaCszMVR-fg";
        public const string TOKEN = "T1==cGFydG5lcl9pZD00NzQ2NDk5MSZzaWc9NDBiMzdhMTVlNDMwOTNiNmVmNTRlODFiNzU5OGJmMDFkOGFlOTExYjpzZXNzaW9uX2lkPTFfTVg0ME56UTJORGs1TVg1LU1UWTJNVGN3TURBM01ESTVOMzVEVDFoVE1tRnlTRVZXU25KR2NrSlJlbGhRYUNzek1WUi1mZyZjcmVhdGVfdGltZT0xNjYxNzAwMDc4Jm5vbmNlPTAuMjI2MjkzMjAzMDk4OTc5NSZyb2xlPXB1Ymxpc2hlciZleHBpcmVfdGltZT0xNjYxNzg2NDc4JmluaXRpYWxfbGF5b3V0X2NsYXNzX2xpc3Q9";
        private const int NUM_CHANNELS = 1;
        VideoCapturer Capturer;
        Session Session;
        Publisher Publisher;
        Subscriber subscriber;
        bool Disconnect = false;
        Dictionary<Stream, Subscriber> SubscriberByStream = new Dictionary<Stream, Subscriber>();
        //stores our ndi references
        Dictionary<Stream, IntPtr> NdiInstancePtrByStream = new Dictionary<Stream, IntPtr>();
        private NDIRenderer renderer;
        public MainWindow()
        {

            // This shows how to enumarate the available capturer devices on the system to allow the user of the app
            // to select the desired camera. If a capturer is not provided in the publisher constructor the first available 
            // camera will be used.
            var devices = VideoCapturer.EnumerateDevices();
            if (devices.Count > 0)
            {
                var selectedDevice = devices[0];
                Trace.WriteLine("Using camera: " + devices[0].Name);
                Capturer = selectedDevice.CreateVideoCapturer(VideoCapturer.Resolution.High);
            }
            else
            {
                Trace.WriteLine("Warning: no cameras available, the publisher will be audio only.");
            }

            // We create the publisher here to show the preview when application starts
            // Please note that the PublisherVideo component is added in the xaml file
            Publisher = new Publisher.Builder(Context.Instance)
            {
                Renderer = PublisherVideo,
                Capturer = Capturer
            }.Build();

            if (API_KEY == "" || SESSION_ID == "" || TOKEN == "")
            {
                MessageBox.Show("Please fill out the API_KEY, SESSION_ID and TOKEN variables in the source code " +
                    "in order to connect to the session", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectDisconnectButton.IsEnabled = false;
            }
            else
            {
                Session = new Session.Builder(Context.Instance, API_KEY, SESSION_ID).Build();

                Session.Connected += Session_Connected;
                Session.Disconnected += Session_Disconnected;
                Session.Error += Session_Error;
                Session.StreamReceived += Session_StreamReceived;
                Session.StreamDropped += Session_StreamDropped;

            }

            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var subscriber in SubscriberByStream.Values)
            {
                subscriber.Dispose();
            }
            Publisher?.Dispose();
            Capturer?.Dispose();
            Session?.Dispose();
        }

        private void Session_Connected(object sender, EventArgs e)
        {
            try
            {
                Session.Publish(Publisher);
            }
            catch (OpenTokException ex)
            {
                Trace.WriteLine("OpenTokException " + ex.ToString());
            }
        }

        private void Session_Disconnected(object sender, EventArgs e)
        {
            Trace.WriteLine("Session disconnected");
            SubscriberByStream.Clear();
            SubscriberGrid.Children.Clear();
        }

        private void Session_Error(object sender, Session.ErrorEventArgs e)
        {
            MessageBox.Show("Session error:" + e.ErrorCode, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void UpdateGridSize(int numberOfSubscribers)
        {
            int rows = Convert.ToInt32(Math.Round(Math.Sqrt(numberOfSubscribers)));
            int cols = rows == 0 ? 0 : Convert.ToInt32(Math.Ceiling(((double)numberOfSubscribers) / rows));
            SubscriberGrid.Columns = cols;
            SubscriberGrid.Rows = rows;
        }

        private void Session_StreamReceived(object sender, Session.StreamEventArgs e)
        {
            Trace.WriteLine("Session stream received");

            // .Net interop doesn't handle UTF-8 strings, so do it manually
            // These must be freed later
            IntPtr sourceNamePtr = UTF.StringToUtf8("NDIlib Send Example: "+e.Stream.Id);

            IntPtr groupsNamePtr = IntPtr.Zero;
            // Create an NDI source description using sourceNamePtr and it's clocked to the video.
            NDIlib.send_create_t createDesc = new NDIlib.send_create_t()
            {
                p_ndi_name = sourceNamePtr,
                p_groups = groupsNamePtr,
                clock_video = true,
                clock_audio = true
            };

            // We create the NDI finder instance
            IntPtr ndiPointer = NDIlib.send_create(ref createDesc);
            NdiInstancePtrByStream.Add(e.Stream, ndiPointer);

            renderer = new NDIRenderer(ndiPointer);
    
            SubscriberGrid.Children.Add(renderer);
            UpdateGridSize(SubscriberGrid.Children.Count);
            subscriber = new Subscriber.Builder(Context.Instance, e.Stream)
            {
                Renderer = renderer
            }.Build();
            SubscriberByStream.Add(e.Stream, subscriber);
            subscriber.SubscribeToAudio = true;

            //we will use the nes AudioData Event handler (added on Windows Opentok Client 2.23.1)
            subscriber.AudioData += onAudioData;
           
            try
            {
                Session.Subscribe(subscriber);
            }
            catch (OpenTokException ex)
            {
                Trace.WriteLine("OpenTokException " + ex.ToString());
            }
        }

        private void onAudioData(object sender, Subscriber.AudioDataEventArgs e)
        {
            //sender is a subscriber, so let's cast to that so we can use it easier (intellisense ^_^)
            Subscriber subscriber = (Subscriber)sender;

            //let's get the NDI instance via the stream
            IntPtr ndiSenderInstance = NdiInstancePtrByStream[subscriber.Stream];
            Trace.WriteLine("Sender " + subscriber.Id);
            // This is how many samples we will read. There is a bunch of calculations here to sync correctly, but let's just do SAMPLE_RATE/30
            NDIlib.audio_frame_interleaved_16s_t audio_frame_16 = new NDIlib.audio_frame_interleaved_16s_t()
            {
                // 48kHz in our example
                sample_rate = e.SampleRate,
                // Lets submit stereo although there is nothing limiting us
                no_channels = NUM_CHANNELS,
                no_samples = e.NumberOfSamples,
                // Timecode (synthesized for us !)
                timecode = NDIlib.send_timecode_synthesize,
            };

            IntPtr pointer = Marshal.AllocHGlobal(e.NumberOfSamples * 2 * NUM_CHANNELS);
            Marshal.Copy(e.SampleBuffer, 0, pointer, e.NumberOfSamples * 2 * NUM_CHANNELS);
            audio_frame_16.p_data = pointer;
            NDIlib.util_send_send_audio_interleaved_16s(ndiSenderInstance, ref audio_frame_16);
            Marshal.FreeHGlobal(pointer);
        }

        private void Session_StreamDropped(object sender, Session.StreamEventArgs e)
        {
            Trace.WriteLine("Session stream dropped");
            var subscriber = SubscriberByStream[e.Stream];
            if (subscriber != null)
            {
                SubscriberByStream.Remove(e.Stream);

                //Let's remove the NDI Instance as well 
                NdiInstancePtrByStream.Remove(e.Stream);
                try
                {
                    Session.Unsubscribe(subscriber);
                }
                catch (OpenTokException ex)
                {
                    Trace.WriteLine("OpenTokException " + ex.ToString());
                }

                SubscriberGrid.Children.Remove((UIElement)subscriber.VideoRenderer);
                UpdateGridSize(SubscriberGrid.Children.Count);
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (Disconnect)
            {
                Trace.WriteLine("Disconnecting session");
                try
                {
                    Session.Unpublish(Publisher);
                    Session.Disconnect();
                }
                catch (OpenTokException ex)
                {
                    Trace.WriteLine("OpenTokException " + ex.ToString());
                }
            }
            else
            {
                Trace.WriteLine("Connecting session");
                try
                {
                    Session.Connect(TOKEN);
                }
                catch (OpenTokException ex)
                {
                    Trace.WriteLine("OpenTokException " + ex.ToString());
                }
            }
            Disconnect = !Disconnect;
            ConnectDisconnectButton.Content = Disconnect ? "Disconnect" : "Connect";
        }

    }
}
