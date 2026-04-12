// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Advising;

/// <summary>
/// A strategy that decides whether the framework should generate an additional <em>forwarding constructor</em>
/// when <see cref="IAdviceFactory.IntroduceParameter(IConstructor, string, IType, IPullStrategy?, System.Collections.Immutable.ImmutableArray{Metalama.Framework.Code.DeclarationBuilders.AttributeConstruction}, IConstructorOverloadingStrategy?)"/>
/// mutates a constructor with a required parameter. A forwarding constructor is a compile-time stub that
/// preserves both the source and binary compatibility of the source constructor: it keeps the
/// pre-mutation signature callable, marks itself with <see cref="Metalama.Framework.RunTime.SourceCompatibilityConstructorAttribute"/>,
/// and chains via <c>: this(...)</c> to the now-mutated constructor. For the standard implementation see
/// <see cref="ConstructorOverloadingStrategy"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Cross-Project Operation:</strong> Overloading strategies operate across project boundaries. Custom
/// implementations must be serializable because the strategy is persisted in the transitive advice metadata and
/// invoked again in projects that reference the project where the parameter was introduced. For example, when a
/// parameter is introduced on a base-class constructor in project A and a derived class in project B has its
/// constructor mutated as a result of the pull walk, the same overloading strategy decides whether project B's
/// derived constructor should also receive a forwarding constructor.
/// </para>
/// </remarks>
/// <seealso cref="ConstructorOverloadingStrategy"/>
/// <seealso cref="Metalama.Framework.Code.ConstructorExtensions.IsSourceCompatibilityConstructor"/>
public interface IConstructorOverloadingStrategy : ICompileTimeSerializable
{
    /// <summary>
    /// Gets the action the framework should apply when a constructor has just been mutated — whether to generate
    /// a forwarding constructor, optionally decorated with <see cref="System.ObsoleteAttribute"/>, or to
    /// do nothing.
    /// </summary>
    /// <param name="mutatedConstructor">The constructor immediately after the new parameter has been appended.</param>
    /// <param name="introducedParameter">The parameter that was just introduced.</param>
    /// <returns>A <see cref="ConstructorOverloadingAction"/> describing the decision and — when
    /// <see cref="ConstructorOverloadingAction.ForwardAndMarkObsolete"/> is used — the <c>[Obsolete]</c>
    /// metadata to emit on the generated forwarding constructor.</returns>
    ConstructorOverloadingAction GetConstructorOverloadingAction( IConstructor mutatedConstructor, IParameter introducedParameter );
}
