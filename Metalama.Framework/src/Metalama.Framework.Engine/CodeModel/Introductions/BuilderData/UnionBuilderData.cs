// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

/// <summary>
/// The immutable data of an introduced union.
/// </summary>
/// <remarks>
/// <para>
/// The two members below are synthesized by the compiler from the union declaration and are emitted by nothing.
/// They are stored here so that the facet can resolve them in any compilation that knows the union:
/// <see cref="INamedType.Constructors"/> and <see cref="INamedType.Properties"/> are empty in a compilation to which
/// the transformations that register them have not been applied, and an aspect that reads the facet of a union it
/// has just introduced reads it in exactly such a compilation.
/// </para>
/// </remarks>
internal sealed class UnionBuilderData : NamedTypeBuilderData
{
    /// <summary>
    /// Gets the constructor of each case of the union, in the order in which the aspect added the cases. The single
    /// parameter of the constructor carries the type of the case.
    /// </summary>
    public ImmutableArray<IFullRef<IConstructor>> CaseConstructors { get; }

    /// <summary>
    /// Gets the property holding the value of the case that the union carries.
    /// </summary>
    public IFullRef<IProperty> ValueProperty { get; }

    public UnionBuilderData( UnionBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this.CaseConstructors = builder.CaseConstructorBuilders.SelectAsImmutableArray( c => c.BuilderData.ToRef() );
        this.ValueProperty = builder.ValueProperty.AssertNotNull().BuilderData.ToRef();
    }
}
