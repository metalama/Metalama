using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.NamespaceFabricAddAspects
{
    internal class Fabric : NamespaceFabric
    {
        public override void AmendNamespace( INamespaceAmender amender )
        {
            amender
                .SelectMany( c => c.DescendantsAndSelf() )
                .SelectMany( t => t.Types )
                .SelectMany( t => t.Methods )
                .Where( m => m.ReturnType.IsConvertibleTo( typeof(string) ) )
                .AddAspect<Aspect>();
        }
    }

    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "overridden" );

            return meta.Proceed();
        }
    }

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