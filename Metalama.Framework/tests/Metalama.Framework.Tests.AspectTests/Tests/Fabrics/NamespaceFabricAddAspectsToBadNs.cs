using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.NamespaceFabricAddAspectsToBadNs2;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.NamespaceFabricAddAspectsToBadNs
{
    internal class Fabric : NamespaceFabric
    {
        public override void AmendNamespace( INamespaceAmender amender )
        {
            amender
                .SelectMany<INamedType>( c => [(INamedType)TypeFactory.GetType( typeof(C2) )] )
                .AddAspect<Aspect>();
        }
    }

    internal class Aspect : TypeAspect { }

    internal class TargetCode
    {
        private int Method1( int a ) => a;

        private string Method2( string s ) => s;
    }

    namespace Sub
    {
        internal class AnotherClass
        {
            private int Method1( int a ) => a;

            private string Method2( string s ) => s;
        }
    }
}

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.NamespaceFabricAddAspectsToBadNs2
{
    public class C2 { }
}