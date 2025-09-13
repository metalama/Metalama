// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.PartialProperties;

public partial class Aspect : IAspect
{
    [Template]
    private partial int Template { get; set; }

    private partial int Template { get => 0; set { } }

    private partial int RunTime { get; set; }

    private partial int RunTime { get => 0; set { } }

    [CompileTime]
    private partial int CompileTime { get; set; }

    private partial int CompileTime { get => 0; set { } }

    [Template]
    private partial int this[int i] { get; set; }

    private partial int this[int i] { get => 0; set { } }
}

#endif