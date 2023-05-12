using FMOD;
using System.Numerics;

namespace Somnium.Framework.Audio
{
#if FMOD
    public struct SoundEffectInstance : ISoundEffectInstance
    {
        /// <summary>
        /// The issue is that channels are reused for multiple sounds when the first has finished playing,
        /// as expected. Thus, we need to keep track of the generation of the sound
        /// </summary>
        public uint generation { get; private set; }
        /// <summary>
        /// The channel that the sound effect is currently playing in
        /// </summary>
        public Channel channel { get; private set; }
        /// <summary>
        /// The sound effect asset that the sound originated from
        /// </summary>
        public SoundEffect soundEffect { get; private set; }
        public SoundEffectInstance(Channel channel, uint generation, SoundEffect soundEffect) 
        { 
            this.channel = channel; 
            this.generation = generation;
            this.soundEffect = soundEffect; 
        }
        public bool Paused
        {
            get
            {
                channel.getPaused(out var paused);
                return paused;
            }
            set
            {
                RESULT result = channel.setPaused(value);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to pause FMOD Sound! Error: " + result.ToString());
                }
            }
        }
        /// <summary>
        /// 0 is never a valid generation
        /// </summary>
        public bool IsValid
        {
            get
            {
                uint expectedGeneration = AudioEngine.ChannelGenerations[channel.handle];
                return generation == expectedGeneration && expectedGeneration != 0 && IsPlaying;
            }
        }
        public float Volume
        {
            get
            {
                RESULT result = channel.getVolume(out var volume);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to get FMOD sound effect instance volume! Error: " + result.ToString());
                }
                return volume;
            }
            set
            {
                RESULT result = channel.setVolume(value);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to set FMOD sound effect instance volume! Error: " + result.ToString());
                }
            }
        }
        public float Pitch
        {
            get
            {
                var result = channel.getPitch(out var pitch);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to get FMOD sound pitch! Error: " + result.ToString());
                }
                return pitch;
            }
            set
            {
                var result = channel.setPitch(value);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to set FMOD sound pitch! Error: " + result.ToString());
                }
            }
        }
        public Vector3 Position
        {
            get
            {
                var result = channel.get3DAttributes(out VECTOR pos, out VECTOR vel);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to get FMOD sound attributes! Error: " + result.ToString());
                }
                return new Vector3(pos.x, pos.y, pos.z);
            }
            set
            {
                var pos = new VECTOR();
                pos.x = value.X;
                pos.y = value.Y;
                pos.z = value.Z;
                var vel = Velocity.ToVECTOR();
                var result = channel.set3DAttributes(ref pos, ref vel);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to set FMOD sound attributes! Error: " + result.ToString());
                }
            }
        }
        public Vector3 Velocity
        {
            get
            {
                var result = channel.get3DAttributes(out VECTOR pos, out VECTOR vel);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to get FMOD sound attributes! Error: " + result.ToString());
                }
                return new Vector3(vel.x, vel.y, vel.z);
            }
            set
            {
                var vel = new VECTOR();
                vel.x = value.X;
                vel.y = value.Y;
                vel.z = value.Z;
                var pos = Position.ToVECTOR();
                var result = channel.set3DAttributes(ref pos, ref vel);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to set FMOD sound attributes! Error: " + result.ToString());
                }
            }
        }
        public int LoopCount
        {
            get
            {
                var result = channel.getLoopCount(out int loopCount);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to get FMOD sound loop count! Error: " + result.ToString());
                }
                return loopCount;
            }
            set
            {
                var result = channel.setLoopCount(value);
                if (result != RESULT.OK)
                {
                    throw new ExecutionException("Failed to set FMOD sound loop count! Error: " + result.ToString());
                }
            }
        }
        /// <summary>
        /// Also marks the channel as complete and thus may be reused by the sound engine
        /// </summary>
        /// <exception cref="ExecutionException"></exception>
        public void Stop()
        {
            var result = channel.stop();
            if (result != RESULT.OK)
            {
                throw new ExecutionException("Failed to stop FMOD Sound! Error: " + result.ToString());
            }
        }
        public void Seek(float time)
        {
            var result = channel.setPosition((uint)(time * 1000f), TIMEUNIT.MS);
            if (result != RESULT.OK)
            {
                throw new ExecutionException("Failed to set FMOD sound progress! Error: " + result.ToString());
            }
        }
        public float GetPlaybackPosition()
        {
            var result = channel.getPosition(out var ms, TIMEUNIT.MS);
            
            if (result != RESULT.OK)
            {
                throw new ExecutionException("Failed to get FMOD sound playback position! Error: " + result);
            }
            return ms / 1000f;
        }
        public bool IsPlaying
        {
            get
            {
                RESULT result;
                if ((result = channel.isPlaying(out var playing)) != RESULT.OK)
                {
                    if (result == RESULT.ERR_INVALID_HANDLE)
                    {
                        return false;
                    }
                    throw new ExecutionException("Error getting playing state of FMOD sound! Error: " + result.ToString());
                }
                return playing;
            }
        }
        public bool IsComplete
        {
            get
            {
                if (!IsValid) return true;

                float duration = soundEffect.GetDuration();
                float playback = GetPlaybackPosition();
                if (playback >= duration)
                {
                    return true;
                }
                return false;
            }
        }
    }
#endif
}
