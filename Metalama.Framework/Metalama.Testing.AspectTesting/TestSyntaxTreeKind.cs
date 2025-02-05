// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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