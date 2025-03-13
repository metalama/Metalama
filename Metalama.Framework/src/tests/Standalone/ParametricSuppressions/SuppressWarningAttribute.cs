// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace ParametricSuppressions;

public class SuppressWarningAttribute : MethodAspect
{
    private static readonly SuppressionDefinition _suppression1 = new( "CS0219" );

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Diagnostics.Suppress( _suppression1.WithFilter( diag => diag.Arguments.Any( a => a is string s && s == "x" ) ), builder.Target );
    }
}

internal class TargetClass
{
    [SuppressWarning]
    private void M()
    {
        var x = 0;
    }
}