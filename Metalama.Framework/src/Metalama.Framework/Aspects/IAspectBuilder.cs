// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using Metalama.Framework.Serialization;
using System;
using System.Threading;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Provides the context and operations for building an aspect in the <see cref="IAspect{T}.BuildAspect"/> method.
    /// This is the primary API for implementing aspect behavior at compile time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="IAspectBuilder"/> is the central API that aspects use to transform code, report diagnostics, and manage aspect composition.
    /// It is passed to your aspect's <see cref="IAspect{T}.BuildAspect"/> method when the aspect is being applied to a target declaration.
    /// </para>
    /// <para>
    /// <b>Key capabilities:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Advising (code transformation):</b> Use extension methods from <see cref="AdviserExtensions"/> such as
    /// <c>builder.Override()</c>, <c>builder.IntroduceMethod()</c>, <c>builder.ImplementInterface()</c>, etc.</description></item>
    /// <item><description><b>Child aspects:</b> Apply additional aspects to related declarations via <see cref="IAspectBuilder{T}.Outbound"/></description></item>
    /// <item><description><b>Diagnostics:</b> Report errors, warnings, or suppress diagnostics through <see cref="IAdviser.Diagnostics"/></description></item>
    /// <item><description><b>Validation:</b> Register validators that run after all aspects have been applied</description></item>
    /// <item><description><b>Context access:</b> Read the <see cref="IAspectBuilder{T}.Target"/> declaration, <see cref="Project"/>, and <see cref="AspectInstance"/> metadata</description></item>
    /// <item><description><b>Aspect control:</b> Skip aspect application via <see cref="SkipAspect"/> or store instance-specific data in <see cref="AspectState"/></description></item>
    /// </list>
    /// <para>
    /// <b>Target vs AdvisedTarget:</b> The <see cref="IAspectBuilder{T}.Target"/> property provides the declaration in its original state
    /// (before the current aspect was applied), while <see cref="IAspectBuilder{T}.AdvisedTarget"/> includes modifications made by the current aspect.
    /// </para>
    /// <para>
    /// This is the non-generic base interface. Use the strongly-typed <see cref="IAspectBuilder{T}"/> variant in your
    /// <see cref="IAspect{T}.BuildAspect"/> implementation for type-safe access to the target declaration.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAspectBuilder{T}"/>
    /// <seealso cref="IAdviser"/>
    /// <seealso cref="IAspect{T}"/>
    /// <seealso cref="AdviserExtensions"/>
    /// <seealso href="@advising-concepts"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@aspect-design"/>
    /// <seealso href="@architecture"/>
    [CompileTime]
    public interface IAspectBuilder : IAdviser, IDisposable
    {
        /// <summary>
        /// Gets the current <see cref="IProject"/>, which represents the <c>csproj</c> file and allows to share project-local data.
        /// </summary>
        IProject Project { get; }

        /// <summary>
        /// Gets the current <see cref="IAspectInstance"/>, which provides metadata about this aspect instance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property gives access to:
        /// <list type="bullet">
        /// <item><see cref="IAspectPredecessor.Predecessors"/>: The chain of aspects and fabrics that caused this aspect to be applied</item>
        /// <item><see cref="IAspectInstance.SecondaryInstances"/>: Additional instances when multiple instances of the same aspect type are applied to the same declaration (only the primary instance's BuildAspect is called)</item>
        /// <item><see cref="IAspectInstance.AspectState"/>: Custom state data specific to this aspect instance</item>
        /// </list>
        /// </para>
        /// </remarks>
        IAspectInstance AspectInstance { get; }

        /// <summary>
        /// Gets an object that allows to create advice, e.g. overriding members, introducing members, or implementing new interfaces.
        /// </summary>
        [Obsolete( "Use the extension methods provided by the AdviserExtensions class. " )]
        IAdviceFactory Advice { get; }

        /// <summary>
        /// Gets the cancellation token for the current operation. Use this to detect when the build process
        /// has been cancelled and abort long-running operations gracefully.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Cancellation can occur during user-initiated build cancellations or at design time when Metalama detects source code changes
        /// in the editor (e.g., the user is typing). To maintain IDE responsiveness, it is important that <see cref="IAspect{T}.BuildAspect"/>
        /// executes quickly and checks for cancellation regularly.
        /// </para>
        /// <para>
        /// Call <see cref="CancellationToken.ThrowIfCancellationRequested"/> periodically during expensive operations
        /// to respond to cancellation requests promptly.
        /// </para>
        /// </remarks>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Cancels the application of this aspect to the target declaration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this method is called, any advice and child aspects added by this aspect are ignored, but diagnostics
        /// already reported are preserved. In aspects with multiple layers (see <see cref="LayersAttribute"/>), subsequent
        /// layers are also skipped.
        /// </para>
        /// <para>
        /// <b>Note:</b> Reporting an error diagnostic automatically causes the aspect to be skipped, so you don't need to
        /// call this method explicitly after reporting errors.
        /// </para>
        /// </remarks>
        /// <seealso cref="IsAspectSkipped"/>
        void SkipAspect();

        /// <summary>
        /// Gets a value indicating whether the <see cref="SkipAspect"/> method was called.
        /// </summary>
        bool IsAspectSkipped { get; }

        /// <summary>
        /// Gets or sets an arbitrary object that is then exposed on the <see cref="IAspectInstance.AspectState"/> property of
        /// the <see cref="IAspectInstance"/> interface. While a single instance of an aspect class can be used for
        /// several target declarations, the <see cref="AspectState"/> is specific to the target declaration. If the aspect
        /// is inherited, the <see cref="AspectState"/> must be compile-time-serializable (<see cref="ICompileTimeSerializable"/> or
        /// default serializable classes).
        /// </summary>
        IAspectState? AspectState { get; set; }

        /// <summary>
        /// Gets the name of the layer being built, or <c>null</c> if this is the default (initial) layer.
        /// When an aspect has several layers, the <see cref="IAspect{T}.BuildAspect"/> method is called several times. To register
        /// aspect layers, add the <see cref="LayersAttribute"/> custom attribute to the aspect class.
        /// </summary>
        string? Layer { get; }

        /// <summary>
        /// Returns a copy of the current <see cref="IAspectBuilder"/>, for use in the current execution context,
        /// but for a different <see cref="IAdviser.Target"/> declaration.
        /// </summary>
        new IAspectBuilder<T> With<T>( T declaration )
            where T : class, IDeclaration;

        [Obsolete( "Use the With method." )]
        IAspectBuilder<T> WithTarget<T>( T newTarget )
            where T : class, IDeclaration;

        /// <summary>
        /// Gets or sets the tags passed to all advice added by the current <see cref="IAspect{T}.BuildAspect"/> method. These tags
        /// can be consumed from the <see cref="meta.Tags"/> property in template code.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Advice always receive the <i>last</i> value of the property, when the <see cref="IAspect{T}.BuildAspect"/> exits.
        /// These tags are merged with the ones passed as an argument of the <c>tags</c> parameter of any advise method.
        /// In case of conflict, the values passed to the advise method win.
        /// </para>
        /// <para>
        /// Tags are useful for passing contextual information from <see cref="IAspect{T}.BuildAspect"/> to template code,
        /// such as configuration values or aspect-specific metadata that templates need at code generation time.
        /// </para>
        /// </remarks>
        /// <seealso cref="meta.Tags"/>
        /// <seealso href="@sharing-state-with-advice"/>
        object? Tags { get; set; }
    }

    /// <summary>
    /// An object used by the <see cref="IAspect{T}.BuildAspect"/> method of the aspect to provide advice, child
    /// aspects and validators, or report diagnostics. This is a strongly-typed variant of the <see cref="IAspectBuilder"/> interface.
    /// </summary>
    /// <seealso cref="IAspectBuilder"/>
    /// <seealso cref="IAdviser{T}"/>
    /// <seealso cref="IAspect{T}"/>
    /// <seealso cref="AdviserExtensions"/>
    /// <seealso href="@aspects"/>
    /// <seealso href="@aspect-design"/>
    public interface IAspectBuilder<out TAspectTarget> : IAspectBuilder, IAdviser<TAspectTarget>
        where TAspectTarget : class, IDeclaration
    {
        /// <summary>
        /// Verifies that the target of the aspect matches an eligibility rule. If not, reports an eligibility error (unless the aspect can be used by inheritance) and skips the aspect.  
        /// </summary>
        /// <param name="rule">An eligibility rule created by <see cref="EligibilityRuleFactory"/>. For performance reasons, it is recommended that you store the rule in a static
        /// field of the aspect.</param>
        /// <returns><c>true</c> if the aspect target qualifies for the given rule, otherwise <c>false</c> (in this case, the <see cref="IAspectBuilder.SkipAspect"/> method is automatically called. </returns>
        bool VerifyEligibility( IEligibilityRule<TAspectTarget> rule );

        /// <summary>
        /// Gets the declaration to which the aspect was added in the state it was <i>before</i> the aspect was applied.
        /// </summary>
        /// <seealso cref="AdvisedTarget"/>
        new TAspectTarget Target { get; }

        /// <summary>
        /// Gets the <see cref="Target"/> declaration in the <i>current</i> state, including all modifications performed by the current aspect.
        /// </summary>
        TAspectTarget AdvisedTarget { get; }

        /// <summary>
        /// Gets an object that provides fabric-like capabilities for adding child aspects, validators, and diagnostics to code
        /// transformed by later aspects. This allows aspects to work with the final transformed code, not just the immediate target.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="Outbound"/> property functions like having a fabric embedded within your aspect. It allows you to:
        /// </para>
        /// <list type="bullet">
        /// <item><description><b>Add child aspects:</b> Apply aspects to related declarations using <c>Outbound.SelectMany(...).AddAspect()</c></description></item>
        /// <item><description><b>Register validators:</b> Validate code after all aspects have been applied using <c>Outbound.Validate()</c></description></item>
        /// <item><description><b>Validate references:</b> Check how other code references the target using <c>Outbound.ValidateInboundReferences()</c></description></item>
        /// <item><description><b>Report diagnostics:</b> Report or suppress diagnostics on the final transformed code</description></item>
        /// </list>
        /// <para>
        /// <b>Key difference from builder:</b> Operations performed through <c>Outbound</c> execute <i>after</i> all aspects ordered
        /// after the current aspect have completed. This lets you work with the fully transformed code rather than just the immediate state.
        /// In contrast, advice added directly through the builder (e.g., <c>builder.Override()</c>) works with the code in its current state.
        /// </para>
        /// <para>
        /// <b>Typical usage pattern:</b> Use <c>Outbound</c> to add child aspects to members of the target type, validate final code structure,
        /// or ensure that introduced members are used correctly after all transformations.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Add a logging aspect to all methods in the target type
        /// builder.Outbound.SelectMany(t => t.Methods).AddAspect&lt;LoggingAspect&gt;();
        ///
        /// // Validate that the transformed code meets requirements
        /// builder.Outbound.Validate(declaration => {
        ///     if (declaration.Methods.Count == 0)
        ///         builder.Diagnostics.Report(MyDiagnostics.NoMethods);
        /// });
        /// </code>
        /// </example>
        /// <seealso href="@child-aspects"/>
        /// <seealso href="@aspect-validating"/>
        /// <seealso href="@fabrics"/>
        IQuery<TAspectTarget> Outbound { get; }

        new IAspectBuilder<T> With<T>( T declaration )
            where T : class, IDeclaration;

        [Obsolete( "Use the With method." )]
        new IAspectBuilder<T> WithTarget<T>( T newTarget )
            where T : class, IDeclaration;
    }
}