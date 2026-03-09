// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Remove_DocComment;

public class MyAspect : Aspect, IAspect<IDeclaration>
{
    public void BuildEligibility( IEligibilityBuilder<IDeclaration> builder ) { }

    public void BuildAspect( IAspectBuilder<IDeclaration> builder )
    {
        builder.RemoveAttributes( GetType() );
    }
}

/// <summary>
/// This is a documented class.
/// </summary>
[MyAspect]
internal class C
{
    /// <summary>
    /// A documented method.
    /// </summary>
    [MyAspect]
    internal void M() { }
}
