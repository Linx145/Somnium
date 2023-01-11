﻿using Silk.NET.Vulkan;
using Somnium.Framework;
using System.Collections.Generic;

namespace Somnium.Framework.Vulkan
{
    public static class VkVertex
    {
        public static Format FromVertexElementFormat(VertexElementFormat format)
        {
            switch (format)
            {
                default:
                    return Format.Undefined;

                case VertexElementFormat.Float:
                    return Format.R32Sfloat;

                case VertexElementFormat.Vector2:
                    return Format.R32G32Sfloat;

                case VertexElementFormat.Vector3:
                    return Format.R32G32B32Sfloat;

                case VertexElementFormat.Vector4:
                    return Format.R32G32B32A32Sfloat;
            }
        }
        public static void RegisterVertex(VertexDeclaration declaration)
        {
            //describes the vertex declaration
            VertexInputBindingDescription bindingDescription = new VertexInputBindingDescription();
            bindingDescription.Binding = declaration.binding;
            if (declaration.inputRate == VertexElementInputRate.Instance)
            {
                bindingDescription.InputRate = VertexInputRate.Instance;
            }
            else bindingDescription.InputRate = VertexInputRate.Vertex;
            bindingDescription.Stride = declaration.size;

            //describes the elements of the vertex
            VertexInputAttributeDescription[] attributeDescriptions = new VertexInputAttributeDescription[declaration.elements.Count];
            for (int i = 0; i < attributeDescriptions.Length; i++)
            {
                attributeDescriptions[i].Binding = declaration.elements[i].binding;
                attributeDescriptions[i].Location = declaration.elements[i].location;
                attributeDescriptions[i].Format = FromVertexElementFormat(declaration.elements[i].format);
                attributeDescriptions[i].Offset = declaration.elements[i].offset;
            }

            

            declaration.registered = true;
        }
    }
}
