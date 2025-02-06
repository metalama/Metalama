// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters.Utilities;

/// <summary>
/// Defines a <see cref="Visit{TValue}(string, TValue, ref TState)"/> method invoked by <see cref="UnknownObjectAccessor.VisitProperties{TState}(IUnknownObjectPropertyVisitor{TState}, ref TState)"/>.
/// </summary>
/// <typeparam name="TState"></typeparam>
[PublicAPI]
public interface IUnknownObjectPropertyVisitor<TState>
{
    /// <summary>
    /// The method invoked by <see cref="UnknownObjectAccessor.VisitProperties{TState}(IUnknownObjectPropertyVisitor{TState}, ref TState)"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of the property value.</typeparam>
    /// <param name="name">Property name.</param>
    /// <param name="value">Property value.</param>
    /// <param name="state">The opaque state passed to <see cref="UnknownObjectAccessor.VisitProperties{TState}(IUnknownObjectPropertyVisitor{TState}, ref TState)"/>.</param>
    void Visit<TValue>( string name, TValue value, ref TState state );

    /// <summary>
    /// Determines if a given property must be visited. If this method returns true, the property is not evaluated.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="state">The opaque state passed to <see cref="UnknownObjectAccessor.VisitProperties{TState}(IUnknownObjectPropertyVisitor{TState}, ref TState)"/>.</param>
    /// <returns></returns>
    bool MustVisit( string name, ref TState state );
}