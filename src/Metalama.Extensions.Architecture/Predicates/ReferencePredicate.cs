// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Extensions.Validation;
using Metalama.Framework.Serialization;

namespace Metalama.Extensions.Architecture.Predicates;

/// <summary>
/// The base class for predicates that applies to a <see cref="ReferenceValidationContext"/>. Standard extension methods
/// are provided on <see cref="ReferencePredicateExtensions"/>.
/// You can create your own predicates by deriving a new class from this one and exposing the API
/// as a extension methods of <see cref="ReferencePredicateBuilder"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This class implements <see cref="ICompileTimeSerializable"/>. All predicates must therefore be serializable.
/// </para>
/// </remarks>
[PublicAPI]
public abstract class ReferencePredicate : ICompileTimeSerializable
{
    private readonly PredicateModifier? _modifier;

    /// <summary>
    /// Gets the <see cref="ReferencePredicateBuilder"/> from which the current predicate was built,
    /// or <c>null</c> if the object was null without a <see cref="ReferencePredicateBuilder"/>.
    /// </summary>
    [NonCompileTimeSerialized]
    protected internal ReferencePredicateBuilder Builder { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferencePredicate"/> class and provides a <see cref="ReferencePredicateBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ReferencePredicateBuilder"/> from which the predicate was built, or <c>null</c>.
    /// Setting this parameter allows the new object to use used in fluent expressions like <see cref="ReferencePredicateExtensions.Or(Metalama.Extensions.Architecture.Predicates.ReferencePredicate,System.Func{Metalama.Extensions.Architecture.Predicates.ReferencePredicateBuilder,Metalama.Extensions.Architecture.Predicates.ReferencePredicate})"/>.</param>
    protected ReferencePredicate( ReferencePredicateBuilder builder )
    {
        this.Builder = builder;
        this._modifier = builder.Modifier;
    }

    /// <summary>
    /// Gets a value indicating whether the current predicate matches the given <see cref="ReferenceValidationContext"/>,
    /// ignoring any modifier such as <c>And</c>, <c>Or</c> or <c>Not</c>.
    /// </summary>
    protected abstract bool IsMatchCore( ReferenceValidationContext context );

    /// <summary>
    /// Gets a value indicating whether the predicate matches the given <see cref="ReferenceValidationContext"/>,
    /// taking the optional modifier (e.g. an <c>And</c>, <c>Or</c> or <c>Not</c> modifier).
    /// </summary>
    public bool IsMatch( ReferenceValidationContext context )
    {
        var currentResult = this.IsMatchCore( context );

        if ( this._modifier != null )
        {
            return this._modifier.IsMatch( currentResult, context );
        }
        else
        {
            return currentResult;
        }
    }

    /// <summary>
    /// Gets the granularity including the modifiers such as <c>And</c>, <c>Or</c> or <c>Not</c>.
    /// </summary>
    public ReferenceGranularity Granularity => this._modifier?.ModifyGranularity( this.GetGranularity() ) ?? this.GetGranularity();

    /// <summary>
    /// Gets the granularity of validation required by this predicate, effect ignoring any modifier such as <c>And</c>, <c>Or</c> or <c>Not</c>.
    /// For instance, if the predicate only compares the namespace, it should return <see cref="ReferenceGranularity.Namespace"/>.
    /// </summary>
    protected abstract ReferenceGranularity GetGranularity();
}