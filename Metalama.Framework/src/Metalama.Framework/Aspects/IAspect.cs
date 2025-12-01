// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// The base interface for all aspects. Aspects are compile-time components that transform, validate, or enhance code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Aspect classes should not implement this non-generic interface directly. Instead, implement the generic variant
    /// <see cref="IAspect{T}"/> or derive from convenience base classes like <see cref="Aspect"/>, <see cref="TypeAspect"/>,
    /// <see cref="MethodAspect"/>, or other specialized aspect base classes.
    /// </para>
    /// <para>
    /// <b>What aspects can do:</b>
    /// </para>
    /// <list type="number">
    /// <item><description><b>Transform code:</b> Override methods, introduce new members, implement interfaces</description></item>
    /// <item><description><b>Report and suppress diagnostics:</b> Emit errors, warnings, information messages; suppress compiler diagnostics</description></item>
    /// <item><description><b>Validate code:</b> Perform advanced validations on target declarations and their references</description></item>
    /// <item><description><b>Add child aspects:</b> Programmatically apply additional aspects to other declarations</description></item>
    /// <item><description><b>Define eligibility:</b> Specify which declarations the aspect can legally target</description></item>
    /// <item><description><b>Customize IDE experience:</b> Control how the aspect appears in code refactoring menus</description></item>
    /// <item><description><b>Execute in layers:</b> Use <see cref="LayersAttribute"/> to split aspect implementation into multiple ordered execution phases</description></item>
    /// </list>
    /// <para>
    /// <b>Immutability:</b> Aspects must be designed as immutable classes. Never store target-specific state in aspect fields
    /// because the same aspect instance may be reused across multiple target declarations in inheritance or cross-project scenarios.
    /// See <see href="@aspect-design"/> for more details.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAspect{T}"/>
    /// <seealso cref="Aspect"/>
    /// <seealso cref="IAspectBuilder"/>
    /// <seealso cref="ITemplateProvider"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@aspect-design"/>
    [RunTimeOrCompileTime]
    public interface IAspect : ICompileTimeSerializable, ITemplateProvider;

    /// <summary>
    /// The generic base interface for all aspects, where the type parameter specifies the kind of code declaration
    /// the aspect can be applied to.
    /// </summary>
    /// <typeparam name="T">The type of declaration this aspect targets (e.g., <see cref="IMethod"/>, <see cref="INamedType"/>, <see cref="IProperty"/>).</typeparam>
    /// <remarks>
    /// <para>
    /// This is the primary interface that aspect classes implement. The type parameter <typeparamref name="T"/> determines
    /// what kind of code elements (methods, types, properties, etc.) the aspect can be applied to.
    /// </para>
    /// <para>
    /// The <see cref="BuildAspect"/> method is called at compile time to configure how the aspect transforms the target code.
    /// This method receives an <see cref="IAspectBuilder{T}"/> which provides access to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The target declaration being advised</description></item>
    /// <item><description>Extension methods to add advice (transformations) via <see cref="AdviserExtensions"/></description></item>
    /// <item><description>Diagnostic reporting and suppression</description></item>
    /// <item><description>Child aspect application and validation registration</description></item>
    /// </list>
    /// <para>
    /// Aspects can be applied to code in several ways: as custom attributes (by deriving from <see cref="Aspect"/>),
    /// programmatically via fabrics, through aspect inheritance, or as child aspects added by other aspects.
    /// </para>
    /// <para>
    /// For complex aspects requiring multiple execution phases, annotate the aspect class with <see cref="LayersAttribute"/>
    /// to have <see cref="BuildAspect"/> called multiple times in a defined order.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="IAspectInstance"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@aspect-design"/>
    [ForcedGenericRunTimeOrCompileTime]
    public interface IAspect<in T> : IAspect, IEligible<T>
        where T : class, IDeclaration
    {
        /// <summary>
        /// Defines the aspect implementation by adding advice, child aspects, and validators to the target declaration.
        /// </summary>
        /// <param name="builder">An object that provides methods to add transformations (advice), apply child aspects, and register validators.</param>
        /// <remarks>
        /// <para>
        /// This method is called at compile time for each target declaration where the aspect is applied.
        /// Use the <paramref name="builder"/> to add code transformations via extension methods from <see cref="AdviserExtensions"/>
        /// such as <c>builder.Override()</c>, <c>builder.IntroduceMethod()</c>, and others.
        /// </para>
        /// <para>
        /// <b>Immutability requirement:</b> Aspects must be designed as immutable classes. Never store target-specific state
        /// in aspect fields from this method because, in scenarios involving inherited aspects or cross-project validators,
        /// the same aspect instance is reused across multiple target declarations. To pass state to templates, use
        /// <see cref="IAspectBuilder.Tags"/> or compile-time template parameters. See <see href="@sharing-state-with-advice"/>
        /// for details.
        /// </para>
        /// <para>
        /// <b>Multiple invocations:</b> If the aspect class is annotated with <see cref="LayersAttribute"/>, this method
        /// is called multiple times (once per layer) for the same target declaration. Access <see cref="IAspectBuilder.Layer"/>
        /// to determine which layer is currently being built.
        /// </para>
        /// <para>
        /// <b>Template provision:</b> As aspects implement <see cref="ITemplateProvider"/>, you can define template methods
        /// (annotated with <see cref="TemplateAttribute"/>) that are used by advice to generate transformed code.
        /// </para>
        /// </remarks>
        [CompileTime]
        void BuildAspect( IAspectBuilder<T> builder )
#if NET5_0_OR_GREATER
        { }
#else
            ;
#endif
    }
}