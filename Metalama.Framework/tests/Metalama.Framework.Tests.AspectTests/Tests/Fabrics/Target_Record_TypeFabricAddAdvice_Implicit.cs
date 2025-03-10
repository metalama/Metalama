// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.Target_Record_TypeFabricAddAdvice_Implicit
{
    // <target>
    internal record TargetRecord
    {
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