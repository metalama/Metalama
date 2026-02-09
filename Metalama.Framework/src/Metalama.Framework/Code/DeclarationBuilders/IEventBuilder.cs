// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Allows to complete the construction of an event that has been created by an advice.
    /// </summary>
    /// <seealso cref="IEvent"/>
    /// <seealso cref="IMemberBuilder"/>
    /// <seealso cref="AdviserExtensions.IntroduceEvent(IAdviser{INamedType}, string, IntroductionScope, OverrideStrategy, System.Action{IEventBuilder}?, object?)"/>
    /// <seealso href="@introducing-members"/>
    public interface IEventBuilder : IMemberBuilder, IEvent, IHasTypeBuilder
    {
        /// <summary>
        /// Gets or sets the event type (i.e. the type of the delegates handled by this event).
        /// </summary>
        new INamedType Type { get; set; }

        /// <summary>
        /// Gets the <see cref="IMethodBuilder"/> for the event adder.
        /// </summary>
        new IMethodBuilder AddMethod { get; }

        /// <summary>
        /// Gets the <see cref="IMethodBuilder"/> for the event remover.
        /// </summary>
        new IMethodBuilder RemoveMethod { get; }

        /// <summary>
        /// Gets the <see cref="IMethodBuilder"/> for the event raiser, or <c>null</c> if the event cannot be raised
        /// (i.e. for non-field-like events with explicit add/remove accessors).
        /// </summary>
        new IMethodBuilder? RaiseMethod { get; }

        /// <summary>
        /// Gets or sets the initializer expression for the event (i.e., the expression at the right-hand side of the equal sign).
        /// </summary>
        new IExpression? InitializerExpression { get; set; }
    }
}