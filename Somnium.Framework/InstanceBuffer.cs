using Silk.NET.Vulkan;
#if VULKAN
using Somnium.Framework.Vulkan;
#endif
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Somnium.Framework
{
    public class InstanceBuffer
    {
        public bool constructed { get; private set; }
        public bool isDisposed { get; private set; }
        public ulong[] handles;
        private readonly Application application;
        public readonly uint instanceDataSize;
        public readonly uint instanceCount;

        #region Vulkan
        AllocatedMemoryRegion[] memoryRegions;
        #endregion

        public InstanceBuffer(Application application, uint instanceDataSize, uint instanceCount)
        {
            this.application = application;
            this.instanceDataSize = instanceDataSize;
            this.instanceCount = instanceCount;

            Construct();
        }
        public static InstanceBuffer New<T>(Application application, uint instanceCount) where T : unmanaged
        {
            unsafe
            {
                return new InstanceBuffer(application, (uint)sizeof(T), instanceCount);
            }
        }
        public void Construct()
        {
            if (constructed) throw new InvalidOperationException("Buffer already constructed!");
            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                        handles = new ulong[Application.Config.maxSimultaneousFrames];
                        memoryRegions = new AllocatedMemoryRegion[Application.Config.maxSimultaneousFrames];
                        for (int i = 0; i < handles.Length; i++)
                        {
                            Buffer buffer = VkEngine.CreateResourceBuffer(instanceDataSize * instanceCount, BufferUsageFlags.VertexBufferBit);
                            memoryRegions[i] = VkMemory.malloc("Instance Buffer", buffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit | MemoryPropertyFlags.HostCachedBit);
                            handles[i] = buffer.Handle;
                        }
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }
            constructed = true;
        }
        public void SetData<T>(T[] instanceData) where T : unmanaged
        {
            SetData(instanceData, 0, instanceData.Length);
        }
        public void SetData<T>(T[] instanceData, int offset, int Length) where T : unmanaged
        {
            if (!constructed) throw new InvalidOperationException("Attempted to set data to an instance data buffer that has not been constructed yet!");
            if (Length > instanceData.Length)
            {
                throw new IndexOutOfRangeException("Attempting to set data outside of this vertex buffer!");
            }

            switch (application.runningBackend)
            {
#if VULKAN
                case Backends.Vulkan:
                    unsafe
                    {
                            //make sure the thing is bound
                            T* data = memoryRegions[application.Window.frameNumber].Bind<T>();
                            new ReadOnlySpan<T>(instanceData, 0, Length).CopyTo(new Span<T>(data + offset * sizeof(T), Length));
                            //instanceData.AsSpan().CopyTo(new Span<T>(data + offset * sizeof(T), Length));
                        //no need to call unbind
                    }
                    break;
#endif
                default:
                    throw new NotImplementedException();
            }

        }
        public void Dispose()
        {
            if (!isDisposed)
            {
                switch (application.runningBackend)
                {
#if VULKAN
                    case Backends.Vulkan:
                        unsafe
                        {
                            for (int i = 0; i < handles.Length; i++)
                            {
                                VkEngine.DestroyResourceBuffer(new Buffer(handles[i]));
                                //VkEngine.vk.DestroyBuffer(VkEngine.vkDevice, new Buffer(handles[i]), null);
                                if (memoryRegions[i].isBound) memoryRegions[i].Unbind();
                                memoryRegions[i].Free();
                            }
                        }
                        break;
#endif
                    default:
                        throw new NotImplementedException();
                }
                isDisposed = true;
            }
        }
    }
}