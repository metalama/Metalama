// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Diff.WarningLocation
{
    internal class MyAspect : TypeAspect
    {
        private static readonly DiagnosticDefinition _warning =
            new( "MY001", Severity.Warning, "Warning on target type." );

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.Diagnostics.Report( _warning );

            builder.IntroduceMethod( nameof(this.IntroducedMethod) );
        }

        [Template]
        private void IntroducedMethod() { }
    }

    // <target>
    [MyAspect]
    internal partial class FirstClass
    {
        private int a;
    }

    [MyAspect]
    internal partial class SecondClass
    {
        private int b;
    }
}