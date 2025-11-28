// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// A flags enumeration of scenarios in which an aspect can be used, controlling whether aspects are eligible
    /// for direct application, inheritance, or live templates. Values can be combined using bitwise operators.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a flags enumeration (marked with <see cref="FlagsAttribute"/>), allowing fine-grained control over aspect
    /// eligibility across different usage scenarios. Individual scenario values can be combined using bitwise OR (<c>|</c>),
    /// and checked using bitwise AND (<c>&amp;</c>). Use methods like <see cref="EligibilityExtensions.ForScenarios"/> or
    /// <see cref="EligibilityExtensions.ExceptForScenarios"/> to restrict eligibility to specific scenarios.
    /// </para>
    /// <para>
    /// The most common scenario is <see cref="Default"/>, which represents direct application of aspects to declarations.
    /// Use <see cref="Inheritance"/> to control eligibility for derived or overridden declarations, and <see cref="LiveTemplate"/>
    /// for IDE code generation scenarios. The <see cref="All"/> value is the combination of all scenarios.
    /// </para>
    /// </remarks>
    /// <seealso cref="IEligibilityBuilder"/>
    /// <seealso cref="IEligible{T}"/>
    /// <seealso cref="EligibilityExtensions"/>
    /// <seealso href="@eligibility"/>
    [CompileTime]
    [Flags]
    public enum EligibleScenarios
    {
        /// <summary>
        /// The aspect or option cannot be applied to the target declaration in any scenario:
        /// not for direct application, not for inheritance, and not as a live template.
        /// </summary>
        /// <remarks>
        /// This value indicates complete ineligibility. Use it when a declaration fundamentally does not support the aspect.
        /// </remarks>
        None = 0,

        /// <summary>
        /// The aspect or option can be applied to declarations that are derived from or override the target declaration,
        /// but not directly to the target declaration itself.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This scenario is commonly used when applying advice to interface methods or abstract methods. The aspect is not
        /// eligible on the interface or abstract method itself (since they have no implementation), but it is eligible on
        /// the concrete implementations of those methods.
        /// </para>
        /// <para>
        /// Use this scenario when an aspect should propagate to derived types or overriding members through inheritance,
        /// even if the original declaration is not eligible for direct application. Aspects must be marked with
        /// <see cref="Metalama.Framework.Aspects.InheritableAttribute"/> to support inheritance.
        /// </para>
        /// </remarks>
        /// <seealso href="@aspect-inheritance"/>
        Inheritance = 1,

        /// <summary>
        /// The aspect or option can be directly applied to the target declaration.
        /// </summary>
        /// <remarks>
        /// This is the most common scenario and represents normal aspect application. When you check eligibility
        /// without specifying scenarios, this is the default scenario that is checked.
        /// </remarks>
        Default = 2,

        [Obsolete( "Use EligibleScenarios.Default" )]
        Aspect = Default,

        /// <summary>
        /// The aspect or option can be used as a live template on the target declaration for IDE code generation.
        /// </summary>
        /// <remarks>
        /// Live templates enable aspects to be used interactively in the IDE for code generation scenarios.
        /// This scenario is separate from normal aspect application.
        /// </remarks>
        LiveTemplate = 4,

        /// <summary>
        /// The aspect or option can be used in any scenario: direct application, inheritance, and as a live template.
        /// </summary>
        /// <remarks>
        /// This is the combination of all eligibility scenarios (<see cref="Inheritance"/> | <see cref="Default"/> | <see cref="LiveTemplate"/>).
        /// </remarks>
        All = Inheritance | Default | LiveTemplate
    }
}