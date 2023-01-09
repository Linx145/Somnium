using Silk.NET.Vulkan;

namespace Somnium.Framework.Vulkan
{
    /// <summary>
    /// Represents information about a queue, such as it's properties, and the indices of the queues with certain roles
    /// </summary>
    public struct QueueProperties
    {
        public QueueFamilyProperties[] properties;
        /// <summary>
        /// The index of the queue with a graphics bit
        /// </summary>
        public uint? graphicsBitIndex;
        /// <summary>
        /// The index of the queue with a compute bit
        /// </summary>
        public uint? computeBitIndex;
        /// <summary>
        /// The index of the queue with a transfer bit
        /// </summary>
        public uint? transferBitIndex;

        public QueueProperties(QueueFamilyProperties[] properties)
        {
            this.properties = properties;
            graphicsBitIndex = null;
            computeBitIndex = null;
            transferBitIndex = null;
            for (uint i = 0; i < properties.Length; i++)
            {
                ref readonly var property = ref properties[i];
                if ((property.QueueFlags & QueueFlags.GraphicsBit) != 0)
                {
                    graphicsBitIndex = i;
                }
                if ((property.QueueFlags & QueueFlags.ComputeBit) != 0)
                {
                    computeBitIndex = i;
                }
                if ((property.QueueFlags & QueueFlags.TransferBit) != 0)
                {
                    transferBitIndex = i;
                }
            }
        }
    }
}
