using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Issue30557ClassLibrary
{
    public class StringPropertyAspect : TypeAspect
    {

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach ( var property in builder.Target.Properties.Where( p => p.Type.Is( typeof( string ) ) ) )
            {
                builder.Advice.Override( property, nameof( OverrideStringProperty ) );
            }
        }

        [Template]
#pragma warning disable CA1822 // Mark members as static
        public string? OverrideStringProperty
#pragma warning restore CA1822 // Mark members as static
        {
            get
            {
                var result = meta.Proceed();
                return result?.ToUpper();
            }
        }

    }
}