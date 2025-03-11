// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_Variants;

// Tests that none of these cases cause compile-time error.

internal abstract class B : ITemplateProvider
{
    [Template]
    public abstract void M();

    [Template]
    public virtual void M2() { }
}

internal class C : B
{
    public override void M() { }

    public sealed override void M2() { }
}

internal sealed class D : B
{
    public override void M() { }
}

// <target>
internal class T { }