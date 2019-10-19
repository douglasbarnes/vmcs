// ThemedMenuStrip is the implementation of a CustomMenuStrip. Currently, no further adaptations are needed, so this class
// purely stands as an instanceable inheritor class, as CustomMenuStrip is abstract.
namespace debugger.Forms
{
    public class ThemedMenuStrip : CustomMenuStrip
    {
        public ThemedMenuStrip(Layer layer, Emphasis emphasis) : base(layer, emphasis)
        {

        }
    }
}
