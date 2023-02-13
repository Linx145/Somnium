using System.IO;
using System;
using FMOD;
using System.Numerics;

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
        public override ISoundEffectInstance Play(float volume = 1f, float pitch = 1f)
        {
            if (sound.getOpenState(out var openState, out var percentBuffered, out bool starving, out bool diskbusy) == RESULT.OK)
            {
                if (openState == OPENSTATE.LOADING)
                {
                    return null;
                }
            }
            else throw new AssetCreationException("Failed to get FMOD sound state!");

            Channel channel;
            var result = API.playSound(sound, default, false, out channel);
            if (result != RESULT.OK || !channel.hasHandle())
            {
                throw new ExecutionException("Failed to play sound!");
            }
            uint generation = AudioEngine.ChannelGenerations[channel.handle];
            generation++;
            AudioEngine.ChannelGenerations[channel.handle] = generation;
            var instance = new SoundEffectInstance(channel, generation, this);

            if (volume != 1f) instance.Volume = volume;
            if (pitch != 1f) instance.Pitch = pitch;

            return instance;
        }
        public override ISoundEffectInstance Play(Vector3 position, Vector3 velocity)
        {
            throw new NotImplementedException();
        }
        public override float GetDuration()
        {
            sound.getLength(out var length, TIMEUNIT.MS);
            return length / 1000f;
        }
        public override void Dispose()
        {
            if (!isDisposed)
            {
                var result = sound.release();
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Error releasing sound asset!");
                }
            }
        }
    }
#endif
}
