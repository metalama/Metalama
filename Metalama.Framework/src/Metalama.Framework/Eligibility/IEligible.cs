// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Runtime.Serialization;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// An interface that allows aspects to specify to which declarations they can be applied, ensuring predictable behavior
    /// and preventing confusing build errors or incorrect code generation patterns.
    /// </summary>
    /// <typeparam name="T">The type of declaration to which the eligibility applies.</typeparam>
    /// <remarks>
    /// <para>
    /// Defining eligibility provides several benefits:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Predictable behavior:</strong> Users receive clear feedback when applying aspects to unsupported declarations.</description></item>
    /// <item><description><strong>Standard error messages:</strong> All eligibility errors use consistent, understandable messages.</description></item>
    /// <item><description><strong>IDE support:</strong> Only eligible declarations appear in code action and refactoring menus.</description></item>
    /// </list>
    /// <para>
    /// Implement the <see cref="BuildEligibility"/> method to define eligibility rules using the <see cref="IEligibilityBuilder{T}"/>
    /// and extension methods from <see cref="EligibilityExtensions"/>. Common rules include checking if a method is static, abstract,
    /// or has certain parameter types.
    /// </para>
    /// </remarks>
    /// <seealso cref="IEligibilityBuilder{T}"/>
    /// <seealso cref="IAspect{T}"/>
    /// <seealso cref="EligibilityExtensions"/>
    /// <seealso href="@eligibility"/>
    [CompileTime]
    public interface IEligible<in T>
        where T : class, IDeclaration
    {
        /// <summary>
        /// Configures the eligibility of the aspect or attribute by defining rules that determine which declarations
        /// the aspect can be applied to.
        /// </summary>
        /// <param name="builder">An object that allows defining eligibility rules using methods from <see cref="EligibilityExtensions"/>,
        /// such as <see cref="EligibilityExtensions.MustNotBeStatic"/>, <see cref="EligibilityExtensions.MustNotBeAbstract"/>,
        /// or <see cref="EligibilityExtensions.MustSatisfy"/>.</param>
        /// <remarks>
        /// <para>
        /// <strong>Important:</strong> Do not reference instance class members in your implementation of <see cref="BuildEligibility"/>.
        /// This method is called on an instance obtained using <see cref="FormatterServices.GetUninitializedObject"/>, that is,
        /// <i>without invoking the class constructor</i>.
        /// </para>
        /// <para>
        /// Implementations must call the base class implementation if one exists.
        /// </para>
        /// <para>
        /// Use <see cref="EligibilityExtensions.MustSatisfy"/> to define custom eligibility conditions when standard methods
        /// like <c>MustNotBeStatic</c> or <c>MustNotBeAbstract</c> are insufficient. You can also validate related declarations
        /// such as the declaring type (using <see cref="EligibilityExtensions.DeclaringType"/>), return type
        /// (using <see cref="EligibilityExtensions.ReturnType"/>), or parameters (using <see cref="EligibilityExtensions.Parameter"/>).
        /// </para>
        /// <para>
        /// When implementing <see cref="BuildEligibility"/> manually (instead of inheriting from built-in aspect classes like
        /// <see cref="OverrideMethodAspect"/> or <see cref="OverrideFieldOrPropertyAspect"/>), you can use
        /// <see cref="EligibilityRuleFactory.GetAdviceEligibilityRule"/> to retrieve the default eligibility rules for specific
        /// advice kinds and then add only the rules that are specific to your aspect.
        /// </para>
        /// </remarks>
        /// <seealso cref="EligibilityRuleFactory"/>
        /// <seealso href="@eligibility"/>
        [CompileTime]
        void BuildEligibility( IEligibilityBuilder<T> builder )
#if NET5_0_OR_GREATER
        { }
#else
            ;
#endif
    }
}