// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Extensions.DependencyInjection;
using System.Collections.Generic;

// https://github.com/metalama/Metalama/issues/744
// Variant: generic type with internal type argument — should report error.

namespace Metalama.Extensions.DependencyInjection.AspectTests.Bugs.Issue744_GenericArgument;

internal interface IInternalService
{
    void DoWork();
}

public class MyAspect : TypeAspect
{
    [IntroduceDependency]
    private readonly IList<IInternalService> _services;
}

// <target>
[MyAspect]
public class TargetClass
{
    public TargetClass() { }
}
