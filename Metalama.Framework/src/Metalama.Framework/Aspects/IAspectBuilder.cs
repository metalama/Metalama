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
    /// An object used by the <see cref="IAspect{T}.BuildAspect"/> method of the aspect to provide advice, child
    /// aspects and validators, or report diagnostics. This is the weakly-typed variant of the <see cref="IAspectBuilder{T}"/> interface.
    /// </summary>
    [CompileTime]
    public interface IAspectBuilder : IAdviser
    {
        /// <summary>
        /// Gets the current <see cref="IProject"/>, which represents the <c>csproj</c> file and allows to share project-local data.
        /// </summary>
        IProject Project { get; }

        /// <summary>
        /// Gets the current <see cref="IAspectInstance"/>, which gives access to the <see cref="IAspectPredecessor.Predecessors"/>
        /// and the <see cref="IAspectInstance.SecondaryInstances"/> of the current aspect.
        /// </summary>
        IAspectInstance AspectInstance { get; }

        /// <summary>
        /// Gets an object that allows to create advice, e.g. overriding members, introducing members, or implementing new interfaces.
        /// </summary>
        IAdviceFactory Advice { get; }

        /// <summary>
        /// Gets the cancellation token for the current operation.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Skips the application of the aspect to the code. Any provided advice and child aspects are ignored, but provided
        /// diagnostics are preserved. In multi-layer aspects, the next layers of the aspect are skipped. 
        /// </summary>
        /// <remarks>
        /// Note that reporting an error automatically causes the aspect to be skipped.
        /// </remarks>
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
        /// can be consumed from the <c>meta.Tags</c> property.
        /// </summary>
        /// <remarks>
        /// Advice always receive the <i>last</i> value of the property, when the <see cref="IAspect{T}.BuildAspect"/> exits.
        /// These tags are merged with the ones passed as an argument of the <c>tags</c> parameter of any advise method.
        /// In case of conflit, the values passed to the advise method win.
        /// </remarks>
        object? Tags { get; set; }
    }

    /// <summary>
    /// An object used by the <see cref="IAspect{T}.BuildAspect"/> method of the aspect to provide advice, child
    /// aspects and validators, or report diagnostics. This is a strongly-typed variant of the <see cref="IAspectBuilder"/> interface.
    /// </summary>
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
        /// Gets an object that allows to add child advice (even to code added by aspects executed after the current one) and to validate code and code references.
        /// </summary>
        IQuery<TAspectTarget> Outbound { get; }

        new IAspectBuilder<T> With<T>( T declaration )
            where T : class, IDeclaration;

        [Obsolete( "Use the With method." )]
        new IAspectBuilder<T> WithTarget<T>( T newTarget )
            where T : class, IDeclaration;
    }
}