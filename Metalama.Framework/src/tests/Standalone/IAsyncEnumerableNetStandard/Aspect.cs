// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

class Aspect : OverrideMethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        var templates = new MethodTemplateSelector(
            nameof(this.OverrideMethod),
            nameof(this.OverrideAsyncMethod),
            nameof(this.OverrideEnumerableMethod),
            nameof(this.OverrideEnumeratorMethod),
            nameof(this.OverrideAsyncEnumerableMethod),
            nameof(this.OverrideAsyncEnumeratorMethod) );

        builder.Advice.Override( builder.Target, templates );
    }

    public override dynamic? OverrideMethod()
    {
        throw new NotImplementedException();
    }
}

class Target
{
    [Aspect]
    static void M() { }
}