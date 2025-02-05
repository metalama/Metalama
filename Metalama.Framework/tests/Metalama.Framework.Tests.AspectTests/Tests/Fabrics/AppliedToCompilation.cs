// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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