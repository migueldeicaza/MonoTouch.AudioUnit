﻿using System;
using System.Collections.Generic;
using System.Text;

using MonoTouch.AudioToolbox;
using MonoTouch.CoreFoundation;

namespace Monotouch_AudioUnit_PlayingSoundMemoryBased
{
    class ExtAudioBufferPlayer : IDisposable
    {
        #region Variables        
        readonly CFUrl _url;
        readonly int _sampleRate;

        AudioComponent _audioComponent;
        AudioUnit _audioUnit;        
        ExtAudioFile _extAudioFile;
        AudioBufferList _buffer;

        AudioStreamBasicDescription _srcFormat;
        AudioStreamBasicDescription _dstFormat;        

        //long _startingPacketCount;
        long _totalFrames;
        bool _isDone;
        bool _isReverse;
        uint _currentFrame;
        bool _isLoop;
        int  _numberOfChannels;
        #endregion

        #region Properties        
        public long TotalFrames { get { return _totalFrames; } }
        public long CurrentPosition
        {
            set
            {
                long frame = value;
                frame = Math.Min(frame, _totalFrames);
                frame = Math.Max(frame, 0);
                _currentFrame = (uint)frame;                
            }
            get
            {
                return _currentFrame;
            }
        }
        #endregion

        #region Constructor
        public ExtAudioBufferPlayer(CFUrl url)
        {
            _sampleRate = 44100;
            _url = url;

            _isDone = false;
            _isReverse = false;
            _currentFrame = 0;
            _isLoop = false;

            prepareExtAudioFile();
            prepareAudioUnit();
        }
        #endregion

        #region private methods
        void _audioUnit_RenderCallback(object sender, AudioUnitEventArgs args)
        {
            // Getting a pointer to a buffer to be filled
            IntPtr outL = args.Data.Buffers[0].Data;
            IntPtr outR = args.Data.Buffers[1].Data;

            unsafe
            {
                var outLPtr = (int*)outL.ToPointer();
                var outRPtr = (int*)outR.ToPointer();                
                
                var buf0 = (int*)_buffer.Buffers[0].Data;
                int *buf1= (_numberOfChannels == 2) ? (int*)_buffer.Buffers[1].Data : buf0;

                for (int i = 0; i < args.NumberFrames; i++)
                {
                    if (_isDone)
                    {
                        // 0-filling
                        *outLPtr++ = 0;
                        *outRPtr++ = 0;
                    }
                    else 
                    {
                        if (!_isReverse)
                        {
                            // normal play
                            if (_currentFrame >= _totalFrames)
                            {
                                _currentFrame = 0;
                                if (!_isLoop)
                                {
                                    _isDone = true;
                                }
                            }

                            *outLPtr++ = buf0[++_currentFrame];
                            *outRPtr++ = buf1[_currentFrame];
                        }
                        else
                        { 
                            // reverse
                            if (_currentFrame <= 0)
                            {
                                _currentFrame = (uint)( _totalFrames - 1);
                                if (_isLoop)
                                {
                                    _isDone = true;
                                }
                            }
                            *outLPtr++ = buf0[--_currentFrame];
                            *outRPtr++ = buf1[_currentFrame];
                        }
                    }
                }
            }
            if (_isDone)
            {
                Stop();
            }
        }

        void prepareExtAudioFile()
        {
            // Opening Audio File
            _extAudioFile = ExtAudioFile.OpenURL(_url);

            // Getting file data format
            _srcFormat = _extAudioFile.FileDataFormat;

            // Setting the channel number of the output format same to the input format
            _dstFormat = AudioUnitUtils.AUCanonicalASBD(_sampleRate, _srcFormat.ChannelsPerFrame);

            // setting reading format as audio unit cannonical format
            _extAudioFile.ClientDataFormat = _dstFormat;

            // getting total frame
            _totalFrames = _extAudioFile.FileLengthFrames;

            // Aloocating AudoBufferList
            _buffer = new MutableAudioBufferList(_srcFormat.ChannelsPerFrame, (int) (sizeof(uint) * _totalFrames));
            _numberOfChannels = _srcFormat.ChannelsPerFrame;

            // Reading all frame into the buffer
            _extAudioFile.Read((uint)_totalFrames, _buffer);
        }

        void prepareAudioUnit()
        {
            // creating an AudioComponentDescription of the RemoteIO AudioUnit
            AudioComponentDescription cd = new AudioComponentDescription(AudioComponentType.Output,
			                                                             AudioComponentSubType.OutputRemote);
            // Getting AudioComponent using the audio component description
            _audioComponent = AudioComponent.FindComponent(cd);

            // creating an audio unit instance
            _audioUnit = AudioUnit.CreateInstance(_audioComponent);

            // setting audio format
            _audioUnit.SetAudioFormat(_dstFormat, 
                AudioUnitScopeType.Input, 
                0 // Remote Output
                );            

            // setting callback method
            _audioUnit.RenderCallback += new EventHandler<AudioUnitEventArgs>(_audioUnit_RenderCallback);

            _audioUnit.Initialize();
        }
        #endregion

        #region Public methods
        public void Play()
        {
            _isDone = false;
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
            Stop();
            _audioUnit.Dispose();
            _extAudioFile.Dispose();            
        }
        #endregion
    }
}
