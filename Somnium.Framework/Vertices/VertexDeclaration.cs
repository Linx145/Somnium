using Silk.NET.Vulkan;

namespace Somnium.Framework.Vertices
{
    public enum VertexElementFormat
    {
        None, Float, Vector2, Vector3, Vector4
    }
    public enum VertexElementInputRate
    {
        Vertex, Instance
    }
    public struct VertexElement
    {
        public uint binding;
        public uint location;
        public VertexElementFormat format;
        public uint offset;

        public VertexElement(uint location, VertexElementFormat format, uint offset)
        {
            binding = 0;
            this.location = location;
            this.format = format;
            this.offset = offset;
        }
    }
    public class VertexDeclaration
    {
        /// <summary>
        /// Whether the vertex declaration is registered already.
        /// </summary>
        public bool registered { get; internal set; }
        public static bool initialized { get; private set; }

        public List<VertexElement> elements;
        /// <summary>
        /// The index of the vertex in the grand scheme of registered vertex types. Used in Vulkan
        /// </summary>
        public uint binding;
        /// <summary>
        /// The sizeof the vertex structure
        /// </summary>
        public uint size;
        /// <summary>
        /// The input rate, whether the structure should be progressed per vertex or per instance
        /// </summary>
        public VertexElementInputRate inputRate;

        public static List<VertexDeclaration> allVertexDeclarations = new List<VertexDeclaration>();
        private VertexDeclaration()
        {
            elements = new List<VertexElement>();
        }

        public void AddElement(VertexElement element)
        {
            element.binding = binding;
            elements.Add(element);
        }

        public static unsafe VertexDeclaration NewVertexDeclaration<T>(Backends backend) where T : unmanaged
        {
            if (initialized)
            {
                throw new InvalidOperationException("Cannot create new vertex declaration after initialization!");
            }
            if (backend == Backends.Vulkan)
            {
                VertexDeclaration declaration = new VertexDeclaration();
                declaration.binding = (uint)allVertexDeclarations.Count;
                declaration.size = (uint)sizeof(T);
                declaration.inputRate = VertexElementInputRate.Vertex;
                declaration.registered = false;

                allVertexDeclarations.Add(declaration);

                return declaration;
            }
            else throw new NotImplementedException();
        }

        public static void RegisterAllBuffers(Backends backend)
        {
            if (backend == Backends.Vulkan)
            {
                for (int i = 0; i < allVertexDeclarations.Count; i++)
                {

                }
            }
            initialized = true;
        }
        public static void UnregisterAllBuffers(Backends backend)
        {
            if (backend == Backends.Vulkan)
            {

            }
        }
    }
}
