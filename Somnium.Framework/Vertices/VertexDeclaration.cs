using System.Collections.Generic;
using System;

namespace Somnium.Framework
{
    public enum VertexElementFormat
    {
        Float, Vector2, Vector3, Vector4, Int, Color, UInt
    }
    public enum VertexElementInputRate
    {
        Vertex, Instance
    }
    public struct VertexElement
    {
        //public uint binding;
        //public uint location;
        public VertexElementFormat format;
        public uint offset;

        public VertexElement(VertexElementFormat format, uint offset)
        {
            //binding = 0;
            //this.location = location;
            this.format = format;
            this.offset = offset;
        }
    }
    public class VertexDeclaration
    {
        public static bool initialized { get; private set; }

        public List<VertexElement> elements;
        /// <summary>
        /// The sizeof the vertex structure
        /// </summary>
        public uint size;
        /// <summary>
        /// The input rate, whether the structure should be progressed per vertex or per instance
        /// </summary>
        public VertexElementInputRate inputRate;
        /// <summary>
        /// Can be whatever the user slaps into it
        /// </summary>
        public object userDefinedMetadata;

        public static List<VertexDeclaration> allVertexDeclarations = new List<VertexDeclaration>();

        private VertexDeclaration()
        {
            elements = new List<VertexElement>();
        }

        public void AddElement(VertexElement element)
        {
            elements.Add(element);
        }
        public static unsafe VertexDeclaration NewVertexDeclaration<T>(Backends backend, VertexElementInputRate inputRate = VertexElementInputRate.Vertex) where T : unmanaged
        {
            return NewVertexDeclaration(backend, (uint)sizeof(T), inputRate);
        }
        public static unsafe VertexDeclaration NewVertexDeclaration(Backends backend, uint size, VertexElementInputRate inputRate)
        {
            if (initialized)
            {
                throw new InvalidOperationException("Cannot create new vertex declaration after initialization!");
            }
            VertexDeclaration declaration = new VertexDeclaration();
            declaration.size = size;
            declaration.inputRate = inputRate;

            return declaration;
        }

        public static void AddDefaultVertexDeclarations(Backends backend)
        {
            if (allVertexDeclarations == null)
            {
                allVertexDeclarations = new List<VertexDeclaration>();
            }
            var declaration = NewVertexDeclaration<VertexPositionColor>(backend);
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector3, 0));
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector4, 12));
            VertexPositionColor.internalVertexDeclaration = declaration;
            allVertexDeclarations.Add(declaration);

            declaration = NewVertexDeclaration<VertexPositionColorTexture>(backend);
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector3, 0));
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector4, 12));
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector2, 28));
            VertexPositionColorTexture.internalVertexDeclaration = declaration;
            allVertexDeclarations.Add(declaration);

            declaration = NewVertexDeclaration<VertexPositionTextureColor>(backend);
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector3, 0));
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector2, 12));
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector4, 20));
            VertexPositionTextureColor.internalVertexDeclaration = declaration;
            allVertexDeclarations.Add(declaration);

            declaration = NewVertexDeclaration<VertexPositionTexture>(backend);
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector3, 0));
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector2, 12));
            VertexPositionTexture.internalVertexDeclaration = declaration;
            allVertexDeclarations.Add(declaration);

            declaration = NewVertexDeclaration<VertexPositionNormalTexture>(backend);
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector3, 0));
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector3, 12));
            declaration.AddElement(new VertexElement(VertexElementFormat.Vector2, 24));
            VertexPositionNormalTexture.internalVertexDeclaration = declaration;
            allVertexDeclarations.Add(declaration);
        }
    }
}
