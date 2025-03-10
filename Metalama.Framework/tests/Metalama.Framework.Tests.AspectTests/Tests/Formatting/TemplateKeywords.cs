// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.IfStatements.TemplateKeywords
{
    class Aspect : IAspect
    {
        [Template]
        dynamic? Template()
        {
            meta.CompileTime( 0 );
            meta.CompileTime<long>( 0 );
            meta.InsertComment( "Hey" );
            return meta.Proceed();
        }
    }
}
