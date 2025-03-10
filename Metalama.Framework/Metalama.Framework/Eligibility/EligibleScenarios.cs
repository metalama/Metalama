// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// Enumeration of scenarios in which an aspect can be used.
    /// </summary>
    /// <seealso href="@eligibility"/>
    [CompileTime]
    [Flags]
    public enum EligibleScenarios
    {
        /// <summary>
        /// Means that the aspect or option can neither be applied to the target declaration, nor to declarations derived from the the
        /// target declaration, nor, for aspects, as a live template.
        /// </summary>
        None = 0,

        /// <summary>
        /// Means that the aspect or option can be applied to declarations that are derived from the target declaration, but not on
        /// the target declaration itself.
        /// </summary>
        Inheritance = 1,

        /// <summary>
        /// Means that the aspect or option can be applied to the target declaration.
        /// </summary>
        Default = 2,

        [Obsolete( "Use EligibleScenarios.Default" )]
        Aspect = Default,

        /// <summary>
        /// Means that the aspect or option can be used as a live template on the target declaration.
        /// </summary>
        LiveTemplate = 4,

        /// <summary>
        /// Means that the aspect or option can be used in any scenario.
        /// </summary>
        All = Inheritance | Default | LiveTemplate
    }
}