// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_TwoTemplates;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(FirstTemplate), InitializerKind.AfterObjectInitializer );
        builder.AddInitializer( nameof(SecondTemplate), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void FirstTemplate()
    {
        Console.WriteLine( "First!" );
    }

    [Template]
    private void SecondTemplate()
    {
        Console.WriteLine( "Second!" );
    }
}

// <target>
[TheAspect]
public class TargetCode
{
}
