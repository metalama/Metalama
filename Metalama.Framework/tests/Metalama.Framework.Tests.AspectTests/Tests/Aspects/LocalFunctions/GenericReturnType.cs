// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameter.GenericReturnType;

public class Override : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(Template), args: new { T = builder.Target.ReturnType } );
    }

    [Template]
    private T Template<[CompileTime] T>()
    {
        T LocalFunction()
        {
            return meta.Proceed()!;
        }

        return LocalFunction();
    }
}

// <target>
internal class TargetClass
{
    [Override]
    private int Method() => 5;
}