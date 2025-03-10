// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameter.VoidReturnTypeInNonVoid;

public class Override : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        void LocalFunction()
        {
            meta.Proceed();
        }

        LocalFunction();

        return default;
    }
}

// <target>
internal class TargetClass
{
    [Override]
    private int Method()
    {
        return 42;
    }

    [Override]
    private int Method_ExpressionBody() => 42;
}