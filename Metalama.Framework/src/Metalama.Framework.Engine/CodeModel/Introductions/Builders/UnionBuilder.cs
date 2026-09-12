// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using System;
using System.Collections.Generic;
using System.Linq;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Builds a union that an advice introduces, written with the <c>union</c> keyword.
/// </summary>
/// <remarks>
/// <para>
/// The language reports a union declaration as a struct, so the type kind is <see cref="TypeKind.Struct"/> and the
/// union is told apart by <see cref="NamedTypeBuilder.IsUnion"/>.
/// </para>
/// <para>
/// Metalama emits the union declaration, which is the <c>union</c> keyword, the name and the case list, and the
/// compiler synthesizes one constructor per case and the <c>Value</c> property from it. Those members are created
/// here and registered by the advice without being emitted, which is section 4.2 of
/// <c>Metalama.Framework/docs/introducing-types.md</c>. They are created when the builder is frozen, because
/// the cases decide the constructors and an aspect adds those at any point in the callback.
/// </para>
/// </remarks>
internal sealed class UnionBuilder : NamedTypeBuilder, IUnionBuilder
{
    /// <summary>
    /// The name that the compiler gives to the property holding the value of the case that the union carries.
    /// </summary>
    private const string _valuePropertyName = "Value";

    private readonly List<IType> _cases = [];
    private readonly List<ConstructorBuilder> _caseConstructors = [];

    public UnionBuilder( AspectLayerInstance aspectLayerInstance, INamespaceOrNamedType declaringNamespaceOrType, string name )
        : base( aspectLayerInstance, declaringNamespaceOrType, name, TypeKind.Struct )
    {
#if !ROSLYN_5_11_0_OR_GREATER

        // The emission of a union declaration is compiled into the latest Roslyn variant only, for the reason that
        // the setter of NamedTypeBuilder.IsClosed gives. The writer refuses the request instead of producing an
        // ordinary struct, so an aspect never silently obtains a type other than the one it asked for.
        throw new InvalidOperationException(
            $"The type '{name}' cannot be a union because the host that runs Metalama uses a version of Roslyn that does not support the unions of C# 15. At design time, that host is the integrated development environment." );
#endif
    }

    /// <summary>
    /// Always <c>true</c>. This class is the one builder that represents a union, which the language reports as a
    /// struct, so the type kind alone does not tell it apart.
    /// </summary>
    public override bool IsUnion => true;

    public IReadOnlyList<IType> Cases => this._cases;

    /// <summary>
    /// Gets the property holding the value of the case that the union carries. The property is <c>null</c> until
    /// the builder is frozen.
    /// </summary>
    public PropertyBuilder? ValueProperty { get; private set; }

    public void AddCase( IType caseType )
    {
        this.CheckNotFrozen();

        if ( caseType == null )
        {
            throw new ArgumentNullException( nameof(caseType) );
        }

        // The compiler reports the case types of a union as a set, so a duplicate is refused rather than declaring
        // a second case that the introduced type could never report.
        if ( this._cases.Any( c => this.Compilation.Comparers.Default.Equals( c, caseType ) ) )
        {
            throw new ArgumentException(
                $"The type '{caseType}' is already a case of the union '{this.Name}'.",
                nameof(caseType) );
        }

        this._cases.Add( this.Translate( caseType ) );
    }

    public void AddCase( Type caseType )
    {
        this.CheckNotFrozen();

        this.AddCase( this.Compilation.Factory.GetTypeByReflectionType( caseType ) );
    }

    /// <summary>
    /// Gets the immutable data of every member that the compiler synthesizes and that the advice registers in the
    /// code model without emitting it. The builder must be frozen before this method is called.
    /// </summary>
    public IEnumerable<NamedDeclarationBuilderData> GetSynthesizedMemberData()
    {
        foreach ( var constructor in this._caseConstructors )
        {
            yield return constructor.BuilderData;
        }

        if ( this.ValueProperty != null )
        {
            yield return this.ValueProperty.BuilderData;
        }
    }

    protected override void FreezeChildren()
    {
        this.MaterializeSynthesizedMembers();

        base.FreezeChildren();

        foreach ( var constructor in this._caseConstructors )
        {
            constructor.Freeze();
        }

        this.ValueProperty?.Freeze();
    }

    /// <summary>
    /// Creates a builder for the constructor of each case and for the <c>Value</c> property.
    /// </summary>
    private void MaterializeSynthesizedMembers()
    {
        foreach ( var caseType in this._cases )
        {
            var constructor = new ConstructorBuilder( this.AspectLayerInstance, this ) { Accessibility = Accessibility.Public };

            constructor.AddParameter( "value", caseType );

            this._caseConstructors.Add( constructor );
        }

        // The type of the Value property is object, because the compiler declares it as the common base of the
        // cases and the union carries any one of them.
        this.ValueProperty = new PropertyBuilder(
            this.AspectLayerInstance,
            this,
            _valuePropertyName,
            hasGetter: true,
            hasSetter: false,
            isAutoProperty: false,
            hasInitOnlySetter: false,
            hasImplicitGetter: false,
            hasImplicitSetter: false )
        {
            Type = this.Compilation.Factory.GetSpecialType( SpecialType.Object ),
            Accessibility = Accessibility.Public
        };
    }
}
