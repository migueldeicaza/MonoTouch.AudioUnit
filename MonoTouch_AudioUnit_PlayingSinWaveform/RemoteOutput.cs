using System;
using System.Runtime.InteropServices;

using MonoTouch.AudioToolbox;
using MonoTouch.AudioUnit;

namespace Monotouch_AudioUnit_PlayingSinWaveform
{
    class RemoteOutput :IDisposable
    {        
        #region Variables
        const int kAudioUnitSampleFractionBits = 24;
        readonly int _sampleRate;

        AudioComponent _component;
        AudioUnit _audioUnit;
        double _phase;        
        #endregion

        #region Constructor
        public RemoteOutput()
        {            
            _sampleRate = 44100;            

            prepareAudioUnit();
        }
        #endregion

        #region Private methods
        void simulator_callback(object sender, AudioUnitEventArgs args)
        {
            // Generating sin waveform
            double dphai = 440 * 2.0 * Math.PI / _sampleRate;

            // Getting a pointer to a buffer to be filled
            IntPtr outL = args.Data.Buffers[0].Data;
            IntPtr outR = args.Data.Buffers[1].Data;

            // filling sin waveform.
            // AudioUnitSampleType is different between a simulator (float32) and a real device (int32).
            unsafe
            {
                var outLPtr = (float*)outL.ToPointer();
                var outRPtr = (float*)outR.ToPointer();
                for (int i = 0; i < args.NumberFrames; i++)
                {
                    float sample = (float)Math.Sin(_phase) / 2048;
                    *outLPtr++ = sample;
                    *outRPtr++ = sample;
                    _phase += dphai;
                }
            }
            _phase %= 2 * Math.PI;
        }
        // AudioUnit callback function uses this method to use instance variables. 
        // In the static callback method is not convienient because instance variables can not used.
        void device_callback(object sender, AudioUnitEventArgs args)
        {
            // Generating sin waveform
            double dphai = 440 * 2.0 * Math.PI / _sampleRate;

            // Getting a pointer to a buffer to be filled
            IntPtr outL = args.Data.Buffers[0].Data;  
            IntPtr outR = args.Data.Buffers[1].Data;

            //  filling sin waveform.
            // AudioUnitSampleType is different between a simulator (float32) and a real device (int32).
            unsafe
            {
                var outLPtr = (int*)outL.ToPointer();
                var outRPtr = (int*)outR.ToPointer();
                for (int i = 0; i < args.NumberFrames; i++)
                {
                    int sample = (int)(Math.Sin(_phase) * int.MaxValue / 128); // signal waveform format is fixed-point (8.24)
                    *outLPtr++ = sample;
                    *outRPtr++ = sample;
                    _phase += dphai;
                }
            }
            _phase %= 2 * Math.PI;
        }
        void prepareAudioUnit()
        {
            // Getting the RemoteUI AudioComponent
            _component = AudioComponent.FindComponent(AudioTypeOutput.Remote);
           
            // Getting Audiounit
            _audioUnit = AudioUnit.CreateInstance(_component);

            // setting AudioStreamBasicDescription
            int AudioUnitSampleTypeSize;
            if (MonoTouch.ObjCRuntime.Runtime.Arch == MonoTouch.ObjCRuntime.Arch.SIMULATOR)
            {
                AudioUnitSampleTypeSize = sizeof(float);
            }
            else
            {
                AudioUnitSampleTypeSize = sizeof(int);
            }
            AudioStreamBasicDescription audioFormat = new AudioStreamBasicDescription()
            {
                SampleRate = _sampleRate,
                Format = AudioFormatType.LinearPCM,
                //kAudioFormatFlagsAudioUnitCanonical = kAudioFormatFlagIsSignedInteger | kAudioFormatFlagsNativeEndian | kAudioFormatFlagIsPacked | kAudioFormatFlagIsNonInterleaved | (kAudioUnitSampleFractionBits << kLinearPCMFormatFlagsSampleFractionShift),
                FormatFlags = (AudioFormatFlags)((int)AudioFormatFlags.IsSignedInteger | (int)AudioFormatFlags.IsPacked | (int)AudioFormatFlags.IsNonInterleaved | (int)(kAudioUnitSampleFractionBits << (int)AudioFormatFlags.LinearPCMSampleFractionShift)),
                ChannelsPerFrame = 2,
                BytesPerPacket = AudioUnitSampleTypeSize,
                BytesPerFrame = AudioUnitSampleTypeSize,
                FramesPerPacket = 1,
                BitsPerChannel = 8 * AudioUnitSampleTypeSize,
                Reserved = 0
            };
            _audioUnit.SetAudioFormat(audioFormat, AudioUnitScopeType.Input, 0);            

            // setting callback
            if (MonoTouch.ObjCRuntime.Runtime.Arch == MonoTouch.ObjCRuntime.Arch.SIMULATOR)
                _audioUnit.RenderCallback += new EventHandler<AudioUnitEventArgs>(simulator_callback);
            else
                _audioUnit.RenderCallback += new EventHandler<AudioUnitEventArgs>(device_callback);
        }
        #endregion

        #region Public methods
        public void Play()
        {
            _audioUnit.Start();
        }
        public void Stop()
        {
            _audioUnit.Stop();
        }
        #endregion

        #region IDisposable メンバ
        public void Dispose()
        {
            _audioUnit.Dispose();
            _component.Dispose();
        }
        #endregion
    }
}
