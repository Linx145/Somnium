using System.IO;
using System;
using FMOD;

namespace Somnium.Framework.Audio
{
#if FMOD
    public class SoundEffect : BaseSoundEffect
    {
        private static FMOD.System API
        {
            get
            {
                return AudioEngine.API;
            }
        }
        public Sound sound;
        public Channel channel;
        public SoundEffect(string filePath)
        {
            CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
            exinfo.cbsize = MarshalHelper.SizeOf(typeof(CREATESOUNDEXINFO));

            string fullPath = Path.GetFullPath(filePath);
            var result = API.createSound(fullPath, MODE.DEFAULT, ref exinfo, out sound);//(fullPath, MODE.DEFAULT, ref createInfo, out sound);
            if (result != RESULT.OK)
            {
                throw new AssetCreationException("Failed to create sound!");
            }
        }
        public override void Play(float volume, float pitch)
        {
            if (sound.getOpenState(out var openState, out var percentBuffered, out bool starving, out bool diskbusy) == RESULT.OK)
            {
                if (openState == OPENSTATE.LOADING)
                {
                    return;
                }
            }
            else throw new AssetCreationException("Failed to get FMOD sound state!");
            var result = API.playSound(sound, default, false, out channel);
            if (result != RESULT.OK)
            {
                throw new ExecutionException("Failed to play sound!");
            }
        }
        public override void Dispose()
        {
            var result = sound.release();
            if (result != RESULT.OK)
            {
                throw new ExecutionException("Error releasing sound asset!");
            }
        }
    }
#endif
    public abstract class BaseSoundEffect : IDisposable
    {
        public abstract void Play(float volume, float pitch);
        public abstract void Dispose();
    }
}
