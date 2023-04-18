using System.IO;
using System;
using FMOD;
using System.Numerics;
using System.ComponentModel.DataAnnotations;

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
        private float duration = -1f;
        public SoundEffect(string filePath)
        {
            CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
            exinfo.cbsize = MarshalHelper.SizeOf(typeof(CREATESOUNDEXINFO));

            string fullPath = Path.GetFullPath(filePath);
            var result = API.createSound(fullPath, MODE.DEFAULT | MODE.LOOP_NORMAL, ref exinfo, out sound);//(fullPath, MODE.DEFAULT, ref createInfo, out sound);
            if (result != RESULT.OK)
            {
                throw new AssetCreationException("Failed to create sound! Error: " + result.ToString());
            }
        }
        public SoundEffect(byte[] data)
        {
            CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
            exinfo.cbsize = MarshalHelper.SizeOf(typeof(CREATESOUNDEXINFO));
            exinfo.length = (uint)data.Length;

            var result = API.createSound(data, MODE.OPENMEMORY | MODE.LOOP_NORMAL, ref exinfo, out sound);
            if (result != RESULT.OK)
            {
                throw new AssetCreationException("Failed to create sound! Error: " + result.ToString());
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
            if (!AudioEngine.ChannelGenerations.TryGetValue(channel.handle, out var generation))
            {
                generation = 1;
                AudioEngine.ChannelGenerations.Add(channel.handle, generation);
            }
            else
            {
                generation++;
                AudioEngine.ChannelGenerations[channel.handle] = generation;
            }
            var instance = new SoundEffectInstance(channel, generation, this);

            instance.LoopCount = 0;
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
            if (duration < 0f)
            {
                sound.getLength(out var length, TIMEUNIT.MS);
                duration = length / 1000f;
            }
            return duration;
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
