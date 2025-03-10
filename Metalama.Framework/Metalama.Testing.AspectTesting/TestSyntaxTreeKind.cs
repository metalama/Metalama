// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Testing.AspectTesting;

public enum TestSyntaxTreeKind
{
    /// <summary>
    /// A normal test syntax tree with an input.
    /// </summary>
    Default,

    /// <summary>
    /// A syntax tree introduced by an aspect.
    /// </summary>
    Introduced,

    /// <summary>
    /// An auxiliary syntax tree required by the test but that should not be a part of the test output.
    /// </summary>
    Auxiliary,

    /// <summary>
    /// A helper syntax tree added by the pipeline but not by the aspect.
    /// </summary>
    Helper,
}