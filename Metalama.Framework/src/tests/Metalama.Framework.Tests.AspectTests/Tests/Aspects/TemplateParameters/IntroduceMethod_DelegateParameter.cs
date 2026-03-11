// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.IntroduceMethod_DelegateParameter;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(FuncTemplate) );
        builder.IntroduceMethod( nameof(ActionTemplate) );
    }

    [Template]
    private void FuncTemplate( Func<object> f )
    {
        f();
    }

    [Template]
    private void ActionTemplate( Action a )
    {
        a();
    }
}

// <target>
[MyAspect]
internal class Target
{
}
