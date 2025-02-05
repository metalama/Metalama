using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.Target_RecordStruct_TypeFabricAddAdvice_Implicit
{
    // <target>
    internal record struct TargetRecordStruct
    {
        private int Method1( int a ) => a;

        private string Method2( string s ) => s;

        private class Fabric : TypeFabric
        {
            public override void AmendType( ITypeAmender amender )
            {
                foreach (var method in amender.Type.Methods)
                {
                    if (method.IsImplicitlyDeclared)
                    {
                        amender.Advice.Override( method, nameof(Template) );
                    }
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