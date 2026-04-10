// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Advising;

/// <summary>
/// A strategy that decides whether the framework should generate an additional <em>source-compatibility constructor</em>
/// when <see cref="IAdviceFactory.IntroduceParameter(IConstructor, string, IType, IPullStrategy?, System.Collections.Immutable.ImmutableArray{Metalama.Framework.Code.DeclarationBuilders.AttributeConstruction}, IConstructorOverloadingStrategy?)"/>
/// mutates a constructor with a required parameter. A source-compatibility constructor is a compile-time stub that preserves the
/// pre-mutation binary signature of a source constructor and chains, via <c>: this(...)</c>, to the now-mutated constructor.
/// For the standard implementation see <see cref="ConstructorOverloadingStrategy"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Cross-Project Operation:</strong> Overloading strategies operate across project boundaries. Custom
/// implementations must be serializable because the strategy is persisted in the transitive advice metadata and
/// invoked again in projects that reference the project where the parameter was introduced. For example, when a
/// parameter is introduced on a base-class constructor in project A and a derived class in project B has its
/// constructor mutated as a result of the pull walk, the same overloading strategy decides whether project B's
/// derived constructor should also receive a forwarding stub.
/// </para>
/// </remarks>
/// <seealso cref="ConstructorOverloadingStrategy"/>
/// <seealso cref="Metalama.Framework.Code.ConstructorExtensions.IsSourceCompatibilityConstructor"/>
public interface IConstructorOverloadingStrategy : ICompileTimeSerializable
{
    /// <summary>
    /// Gets a value indicating whether a forwarding constructor should be generated for a given mutated constructor.
    /// </summary>
    /// <param name="mutatedConstructor">The constructor immediately after the new parameter has been appended.</param>
    /// <param name="introducedParameter">The parameter that was just introduced.</param>
    /// <returns><c>true</c> if the framework should generate (or extend) a forwarding stub preserving the
    /// pre-mutation signature of <paramref name="mutatedConstructor"/>; otherwise, <c>false</c>.</returns>
    bool ShouldGenerateForwarder( IConstructor mutatedConstructor, IParameter introducedParameter );
}
