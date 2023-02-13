using System;
using System.Numerics;

namespace Somnium.Framework.Audio
{
    public abstract class BaseSoundEffect : IDisposable
    {
        public bool isDisposed { get; private set; }
        /// <summary>
        /// Plays the sound at a specified volume and pitch offset.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        public abstract ISoundEffectInstance Play(float volume, float pitch);
        /// <summary>
        /// Plays the sound at a specified 3D position and velocity
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <returns></returns>
        public abstract ISoundEffectInstance Play(Vector3 position, Vector3 velocity);
        /// <summary>
        /// Returns the length of the sound in seconds
        /// </summary>
        /// <returns></returns>
        public abstract float GetDuration();
        /// <summary>
        /// Disposes of the sound effect.
        /// </summary>
        public abstract void Dispose();
    }
}
