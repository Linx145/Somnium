using Silk.NET.Core;
using Silk.NET.Vulkan;

namespace Somnium.Framework.Vulkan
{
    /// <summary>
    /// Represents information about a queue, such as it's properties, and the indices of the queues with certain roles
    /// </summary>
    public struct QueueProperties
    {
        public QueueFamilyProperties[] properties;

        private uint? generalPurposeQueueIndex;
        private uint? dedicatedGraphicsQueueIndex;
        private uint? dedicatedComputeQueueIndex;
        private uint? dedicatedTransferQueueIndex;

        public QueueProperties(QueueFamilyProperties[] properties)
        {
            this.properties = properties;
            generalPurposeQueueIndex = null;
            dedicatedGraphicsQueueIndex = null;
            dedicatedComputeQueueIndex = null;
            dedicatedTransferQueueIndex = null;
        }

        /// <summary>
        /// Gets the index of a general all-purpose queue.
        /// </summary>
        /// <param name="requiredFlags">If set to anything other than QueueFlags.None, specifies that the function can only return a queue that has all required flags. Otherwise, returns the best queue</param>
        /// <returns></returns>
        public unsafe uint? GetGeneralPurposeQueue(in PhysicalDevice device, QueueFlags requiredFlags = QueueFlags.GraphicsBit, bool mustPresent = true)
        {
            if (generalPurposeQueueIndex != null) return generalPurposeQueueIndex;

            int maxScore = int.MinValue;
            for (uint i = 0; i < properties.Length; i++)
            {

                ref readonly var property = ref properties[i];
                if (requiredFlags == QueueFlags.None || (property.QueueFlags & requiredFlags) != 0)
                {
                    if (mustPresent)
                    {
                        Bool32 isSupported = new Bool32(false);
                        VulkanEngine.KhrSurfaceAPI.GetPhysicalDeviceSurfaceSupport(device, i, VulkanEngine.WindowSurface, &isSupported);
                        if (!isSupported)
                        {
                            continue;
                        }
                    }
                    int score = 0;
                    if ((property.QueueFlags & QueueFlags.GraphicsBit) != 0)
                    {
                        score++;
                    }
                    if ((property.QueueFlags & QueueFlags.ComputeBit) != 0)
                    {
                        score++;
                    }
                    if ((property.QueueFlags & QueueFlags.TransferBit) != 0)
                    {
                        score++;
                    }
                    if ((property.QueueFlags & QueueFlags.SparseBindingBit) != 0)
                    {
                        score++;
                    }

                    if (score > maxScore)
                    {
                        maxScore = score;
                        generalPurposeQueueIndex = i;
                    }
                }
            }
            return generalPurposeQueueIndex;
        }
    }
}
