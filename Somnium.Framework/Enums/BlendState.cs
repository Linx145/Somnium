namespace Somnium.Framework
{
	public enum Blend
    {
		One,
		Zero,
		SourceColor,
		InverseSourceColor,
		SourceAlpha,
		InverseSourceAlpha,
		DestinationColor,
        InverseDestinationColor,
		DestinationAlpha,
        InverseDestinationAlpha
    }
    public class BlendState
    {
        public Blend SourceColorBlend;
        public Blend SourceAlphaBlend;
        public Blend DestinationColorBlend;
        public Blend DestinationAlphaBlend;

        public static readonly BlendState Additive;
        public static readonly BlendState AlphaBlend;
        public static readonly BlendState NonPremultiplied;
        public static readonly BlendState Opaque;
        static BlendState()
        {
            Additive = new BlendState(Blend.SourceAlpha, Blend.One);
            AlphaBlend = new BlendState(Blend.One, Blend.InverseSourceAlpha);
            NonPremultiplied = new BlendState(Blend.SourceAlpha, Blend.InverseSourceAlpha);
            Opaque = new BlendState(Blend.One, Blend.Zero);
        }
        public BlendState(Blend srcBlend, Blend destinationBlend)
        {
            SourceColorBlend = srcBlend;
            SourceAlphaBlend = srcBlend;
            DestinationColorBlend = destinationBlend;
            DestinationAlphaBlend = destinationBlend;
        }
        public BlendState(Blend srcColBlend, Blend srcAlphaBlend, Blend destColorBlend, Blend destAlphaBlend)
        {
            SourceColorBlend = srcColBlend;
            SourceAlphaBlend = srcAlphaBlend;
            DestinationColorBlend = destColorBlend;
            DestinationAlphaBlend = destAlphaBlend;
        }
    }
}
