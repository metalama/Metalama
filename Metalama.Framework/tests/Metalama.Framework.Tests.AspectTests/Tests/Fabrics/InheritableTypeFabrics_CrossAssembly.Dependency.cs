using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.InheritableTypeFabric_CrossAssembly
{
    public class BaseClass
    {
        private int Method1( int a ) => a;

        private string Method2( string s ) => s;

        [Inheritable]
        private class Fabric : TypeFabric
        {
            public override void AmendType( ITypeAmender amender )
            {
                foreach (var method in amender.Type.Methods)
                {
                    amender.Advice.Override( method, nameof(Template) );
                }
            }

            [Template]
            private dynamic? Template()
            {
                Console.WriteLine( "overridden" );

                return meta.Proceed();
            }
        }
    }
}