// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// The verdict of the durability evaluator for a type.
    /// </summary>
    internal enum DurabilityKind
    {
        /// <summary>
        /// The type is safe to be held across compilations.
        /// </summary>
        Durable,

        /// <summary>
        /// The type reaches, or may reach, an object that is bound to a compilation.
        /// </summary>
        NotDurable,

        /// <summary>
        /// The type is an interface or an abstract type that does not carry the attribute. Reported separately from
        /// <see cref="NotDurable"/> because the remedy differs in kind: annotating a class is verified against its own
        /// members, whereas annotating an interface exports the obligation to every implementation, which the analyzer
        /// then verifies in turn. It is not undecidable, but the guarantee reaches only the implementations that are
        /// compiled with this analyzer, so a project may reasonably weigh it differently.
        /// </summary>
        NotAnnotated
    }
}
