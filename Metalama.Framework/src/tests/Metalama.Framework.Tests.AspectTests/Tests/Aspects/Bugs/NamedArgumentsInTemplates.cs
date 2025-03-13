// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.NamedArgumentsInTemplates;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.Target.Method.With( (IExpression)meta.This, InvokerOptions.Default ).Invoke();

        meta.Target.Method.With( target: (IExpression)meta.This, options: InvokerOptions.Default ).Invoke();

        meta.Target.Method.With( options: InvokerOptions.Default, target: (IExpression)meta.This ).Invoke();

        return null;
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void M() { }
}