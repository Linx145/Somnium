using Silk.NET.Vulkan;
using Somnium.Framework;
using System;
using System.Collections.Generic;

namespace Somnium.Framework.Vulkan
{
    public struct VkVertex
    {
        public VertexInputBindingDescription bindingDescription;
        public VertexInputAttributeDescription[] attributeDescriptions;

        public VkVertex(VertexDeclaration declaration)
        {
            //describes the vertex declaration
            bindingDescription = new VertexInputBindingDescription();
            
            if (declaration.inputRate == VertexElementInputRate.Instance)
            {
                bindingDescription.InputRate = VertexInputRate.Instance;
            }
            else bindingDescription.InputRate = VertexInputRate.Vertex;
            bindingDescription.Stride = declaration.size;

            //describes the elements of the vertex
            attributeDescriptions = new VertexInputAttributeDescription[declaration.elements.Count];
            for (int i = 0; i < attributeDescriptions.Length; i++)
            {
                attributeDescriptions[i].Binding = (uint)i;//declaration.elements[i].binding;
                attributeDescriptions[i].Format = Converters.FormatFromVertexElementFormat[(int)declaration.elements[i].format];
                //attributeDescriptions[i].Location = declaration.elements[i].location;
                //attributeDescriptions[i].Format = FromVertexElementFormat(declaration.elements[i].format);
                attributeDescriptions[i].Offset = declaration.elements[i].offset;
            }
        }
    }
}
