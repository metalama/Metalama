// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.CompileTimeParameterWithRunTimeType;

internal class MyAspect : TypeAspect
{
    [Template]
    public void Method<[CompileTime] TC, TR>(
        [CompileTime] TC a,
        [CompileTime] TC[] b,
        [CompileTime] TR c,
        [CompileTime] List<TR> d,
        [CompileTime] Target e ) { }
}

// <target>
[MyAspect]
internal class Target { }