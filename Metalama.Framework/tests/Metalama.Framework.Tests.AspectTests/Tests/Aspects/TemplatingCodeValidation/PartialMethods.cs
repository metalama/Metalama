// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.PartialMethods;

public partial class Aspect : IAspect
{
    [Template]
    partial void Template(int i);

    [Template]
    private partial int Template(string s);

    private partial int Template(string s) => 0;

    partial void RunTimeMethod(int i);

    [CompileTime]
    partial void CompileTimeMethod(int i);
}