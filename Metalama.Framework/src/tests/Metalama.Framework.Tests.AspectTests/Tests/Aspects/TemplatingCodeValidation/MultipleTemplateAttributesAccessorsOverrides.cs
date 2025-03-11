// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.MultipleTemplateAttributesAccessorsOverrides;

public class BaseAspect : TypeAspect
{
    [Template]
    public virtual int P
    {
        get => 42;
    }
}

public class Aspect : BaseAspect
{
    [Template]
    public override int P
    {
        get => 42;
    }
}