// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// To avoid confusion due to some identical type names, this file is concerned only with Roslyn types.
// Do not add usings for the namespaces Metalama.Framework.Code, Metalama.Framework.Engine.CodeModel or
// related namespaces.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

internal partial class DependencyGraphBuilder
{
    private sealed partial class Visitor
    {
        /// <summary>
        /// Describes a section of a member access chain.
        /// </summary>
        /// <remarks>
        /// For example, in <c>A.B.C.D( foo )</c>, if <c>D</c> is
        /// a method which is not supported for dependency analysis, then:
        /// - The stem is <c>A.B</c>
        /// - The leaf is <c>C</c>
        /// - The unsupported section is <c>D</c>.
        /// </remarks>
        [CompileTime]
        private enum ChainSection
        {
            /// <summary>
            /// All but the final supported symbol in a chain.
            /// </summary>
            Stem,

            /// <summary>
            /// The final supported symbol in a chain.
            /// </summary>
            Leaf,

            /// <summary>
            /// All the symbols that follow after <see cref="Stem"/> and <see cref="Leaf"/>.
            /// </summary>
            Unsupported
        }
    }
}