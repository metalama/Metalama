// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Applying.AppliedToCompilation;

[assembly: MyAspect]

namespace Metalama.Framework.IntegrationTests.Aspects.Applying.AppliedToCompilation
{
    public class MyAspect : CompilationAspect
    {
        public override void BuildAspect( IAspectBuilder<ICompilation> builder )
        {
            throw new Exception( "Oops" );
        }
    }

    // <target>
    internal class TargetClass { }
}