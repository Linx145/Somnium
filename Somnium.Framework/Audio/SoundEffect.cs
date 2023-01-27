using System.IO;
using System;

namespace Somnium.Framework.Audio
{
    public abstract class BaseSoundEffect : IDisposable
    {
        public abstract void Play(float volume, float pitch);
        public abstract void Dispose();
    }
}
