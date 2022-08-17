using OpenTok;
using System;
using System.Runtime.InteropServices;
using NewTek;
using System.Diagnostics;
using System.Threading;
using System.Text;

namespace NDISource
{
    class NDIAudioDevice : IAudioDevice
    {
        private const int SAMPLING_RATE = 44100;
        private const int NUM_BITS = 16;
        private const int NUM_CHANNELS = 1;

        //We will make this public so we can pass it to the video renderer
        public AudioDevice.AudioBus audioBus;

        private IntPtr ndiSenderInstance;
        private AudioDeviceSettings capturerSettings;
        private AudioDeviceSettings rendererSettings;

        private bool isCapturerInit;
        private bool isRendererInit;

        private bool isCapturerStarted;
        private bool isRendererStarted;

        private bool shouldKillRendererThread;

        public NDIAudioDevice(IntPtr ndiSenderInstance)
        {
            Console.WriteLine("Starting INIT Audio");
            this.ndiSenderInstance = ndiSenderInstance;
            
        }

        public NDIAudioDevice()
        {
            Console.WriteLine("Starting INIT Audio 2");
           

        }

        public void DestroyAudio()
        {
            Console.WriteLine("Destroying Audio");
            DestroyAudioCapturer();
            DestroyAudioRenderer();
            Console.WriteLine("Audio Destroyed");
        }

        public void DestroyAudioCapturer()
        {
            Console.WriteLine("Destroy Capturer");
            
        }

        public void DestroyAudioRenderer()
        {
            Console.WriteLine("Destroy Renderer");
            
        }

        public AudioDeviceSettings GetAudioCapturerSettings()
        {
            if (capturerSettings == null)
            {
                Console.WriteLine("Creating new audio capturer settings");
                capturerSettings = new AudioDeviceSettings();
                capturerSettings.NumChannels = NUM_CHANNELS;
                capturerSettings.SamplingRate = SAMPLING_RATE;
            }

            Console.WriteLine("Getting Audio Capturer Settings");
            return capturerSettings;
        }

        public AudioDeviceSettings GetAudioRendererSettings()
        {
            if (rendererSettings == null)
            {
                Console.WriteLine("Creating new audio renderer settings");
                rendererSettings = new AudioDeviceSettings();
                rendererSettings.NumChannels = NUM_CHANNELS;
                rendererSettings.SamplingRate = SAMPLING_RATE;
            }

            Console.WriteLine("Getting Audio Renderer Settings");
            return rendererSettings;
        }

        public int GetEstimatedAudioCaptureDelay()
        {
            return 0;
        }

        public int GetEstimatedAudioRenderDelay()
        {
            return 0;
        }

        public void InitAudio(AudioDevice.AudioBus audioBus)
        {
            Console.WriteLine("Init Audio");
            this.audioBus = audioBus;
        }

        public void InitAudioCapturer()
        {
            Console.WriteLine("Init Capturer");
        }

        public void InitAudioRenderer()
        {
            Console.WriteLine("Initializing Audio Renderer");
            isRendererInit = true;
            Console.WriteLine("Audio Renderer Init");
        }

        public bool IsAudioCapturerInitialized()
        {
            return isCapturerInit;
        }

        public bool IsAudioCapturerStarted()
        {
            return isCapturerStarted;
        }

        public bool IsAudioRendererInitialized()
        {
            return isRendererInit;
        }

        public bool IsAudioRendererStarted()
        {
            return isRendererStarted;
        }

        public void StartAudioCapturer()
        {
            Console.WriteLine("Start Capturer");
        }

        public void StartAudioRenderer()
        {
            Console.WriteLine("Audio Renderer Starting");

            shouldKillRendererThread = false;
            Thread thread = new Thread(waveOut_ThreadJob);
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
            isRendererStarted = true;
            Console.WriteLine("Audio Renderer Started");
        }

        public void StopAudioCapturer()
        {
            Console.WriteLine("Audio Capturer Stopping");
            
            isCapturerStarted = false;
            Console.WriteLine("Audio Capturer Stopped");
        }

        public void StopAudioRenderer()
        {
            Console.WriteLine("Audio Renderer Stopping");
            shouldKillRendererThread = false;
            isRendererStarted = false;
            Console.WriteLine("Audio Renderer Stopped");
        }



        private void waveOut_ThreadJob()
        {
            Console.WriteLine("Renderer Thread Started");
           

            while (!shouldKillRendererThread)
            {
                // This is how many samples we will read. There is a bunch of calculations here to sync correctly, but let's just do SAMPLE_RATE/30
                int readRate = SAMPLING_RATE/30;
                NDIlib.audio_frame_interleaved_16s_t audio_frame_16 = new NDIlib.audio_frame_interleaved_16s_t()
                {
                    // 48kHz in our example
                    sample_rate = SAMPLING_RATE,
                    // Lets submit stereo although there is nothing limiting us
                    no_channels = NUM_CHANNELS,
                    
                    no_samples = readRate,
                    // Timecode (synthesized for us !)
                    timecode = NDIlib.send_timecode_synthesize,
                };

                
                IntPtr pointer = Marshal.AllocHGlobal(SAMPLING_RATE * 2 * NUM_CHANNELS);
                int count = audioBus.ReadRenderData(pointer, readRate);

                Console.WriteLine("Read {0} samples from Renderer", count);

                byte[] buffer = new byte[count * 2 * NUM_CHANNELS];
                Marshal.Copy(pointer, buffer, 0, count * 2 * NUM_CHANNELS);
                audio_frame_16.p_data = pointer;
                Console.WriteLine("BUffer {0}", buffer.ToString());
                Console.WriteLine(Encoding.UTF8.GetString(buffer));
                NDIlib.util_send_send_audio_interleaved_16s(ndiSenderInstance, ref audio_frame_16);
                Marshal.FreeHGlobal(pointer);
            }
            Console.WriteLine("Renderer Thread Stopping");
        }
    }
  
}