﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoTouch.AudioToolbox
{
    public class AudioGraphEventArgs : AudioUnitEventArgs
    {
        #region Constructor
        public AudioGraphEventArgs(AudioUnit.AudioUnitRenderActionFlags _ioActionFlags,
            MonoTouch.AudioToolbox.AudioTimeStamp _inTimeStamp,
            uint _inBusNumber,
            uint _inNumberFrames,
            AudioBufferList _ioData)
            : base(_ioActionFlags, _inTimeStamp, _inBusNumber, _inNumberFrames, _ioData)
        {
        }
        #endregion
    }
}
