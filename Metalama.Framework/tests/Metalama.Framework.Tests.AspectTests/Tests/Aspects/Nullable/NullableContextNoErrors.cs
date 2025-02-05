#nullable disable

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Nullable.NullableContextNoErrors
{
    internal class Aspect : TypeAspect
    {
        [Introduce]
        private string Introduced1( string a ) => a.ToString();

#nullable enable
        [Introduce]
        private string Introduced2( string? a ) => a!.ToString();

        [Introduce]
        private string Introduced3( int x )
        {
#nullable disable
            return "";
        }
    }
}