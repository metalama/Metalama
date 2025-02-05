using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.TwoProjectFabrics
{
    internal class Fabric1 : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender
                .SelectMany( c => c.Types )
                .SelectMany( t => t.Methods )
                .Where( m => m.ReturnType.IsConvertibleTo( typeof(string) ) )
                .AddAspect<Aspect>();
        }
    }

    internal class Fabric2 : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender
                .SelectMany( c => c.Types )
                .SelectMany( t => t.Methods )
                .Where( m => m.ReturnType.IsConvertibleTo( typeof(int) ) )
                .AddAspect<Aspect>();
        }
    }

    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "overridden" );
            Console.WriteLine( ( (IFabricInstance)meta.AspectInstance.Predecessors.Single().Instance ).Fabric.ToString() );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method1( int a ) => a;

        private string Method2( string s ) => s;
    }
}