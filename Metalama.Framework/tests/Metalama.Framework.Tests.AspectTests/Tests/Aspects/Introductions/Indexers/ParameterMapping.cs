using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.ParameterMapping
{
    /*
     * Verifies that template parameter is correctly mapped to "value" and parameters are mapped by index.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer(
                new[] { ( typeof(string), "y" ), ( typeof(int), "x" ) },
                nameof(GetTemplate),
                nameof(SetTemplate) );
        }

        [Template]
        public int GetTemplate( string x, int y )
        {
            return x.Length + y;
        }

        [Template]
        public void SetTemplate( string x, int y, int z )
        {
            var q = x.Length + y + z;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}