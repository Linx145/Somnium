﻿using System.Numerics;

namespace Somnium.Framework.Audio
{
    public interface ISoundEffectInstance
    {
        public bool Paused { get; set; }
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public bool IsValid { get; }
        public void Stop();
        public void Seek(float time);
        public bool IsPlaying();
    }
}
