// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using System;
using System.Collections.Generic;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Builds a delegate that an advice introduces.
/// </summary>
/// <remarks>
/// <para>
/// The class derives from <see cref="NamedTypeBuilder"/>, because the compilation model requires an
/// <c>INamedTypeImpl</c>, while the interface it exposes to an aspect is <see cref="IDelegateBuilder"/>, which does
/// not derive from <see cref="INamedType"/>. The public interface is therefore a narrowed view of an object that is
/// a named type internally.
/// </para>
/// <para>
/// The signature lives on an owned <see cref="MethodBuilder"/> named <c>Invoke</c>, in the way
/// <see cref="ExtensionBlockBuilder"/> owns its receiver parameter builder: it is created in the constructor,
/// frozen in <see cref="FreezeChildren"/>, and registered as its own transformation by the advice. The members of
/// <see cref="IDelegateBuilder"/> that describe a signature forward to it, so no logic is duplicated.
/// </para>
/// <para>
/// That method must be named <c>Invoke</c> and nothing else. <c>DelegateFacet</c> resolves it by that literal name
/// through <c>INamedType.Methods</c>, and the name that the code model reports is the one that
/// <c>NamedDeclarationBuilderData</c> snapshots off the builder, so a builder carrying the name of the delegate
/// would make the facet of every introduced delegate fail to resolve. Section 6.1 of
/// <c>Metalama.Framework/docs/introducing-delegates.md</c> records that constraint.
/// </para>
/// <para>
/// The type parameters belong to the delegate and not to the <c>Invoke</c> method, which is what the language
/// declares, so the <c>AddTypeParameter</c> method adds them to the type.
/// </para>
/// </remarks>
internal sealed class DelegateBuilder : NamedTypeBuilder, IDelegateBuilder, ITypeBuilderWithSynthesizedMembers
{
    /// <summary>
    /// The name that the language gives to the method of a delegate, and that <c>DelegateFacet</c> resolves.
    /// </summary>
    private const string _invokeMethodName = nameof(Action.Invoke);

    /// <summary>
    /// Gets the builder of the <c>Invoke</c> method, which carries the signature of the delegate.
    /// </summary>
    public MethodBuilder InvokeMethodBuilder { get; }

    public IEnumerable<NamedDeclarationBuilderData> GetSynthesizedMemberData()
    {
        // The compiler synthesizes the Invoke method from the delegate declaration, which has no member list to put
        // one in.
        yield return this.InvokeMethodBuilder.BuilderData;
    }

    public DelegateBuilder( AspectLayerInstance aspectLayerInstance, INamespaceOrNamedType declaringNamespaceOrType, string name )
        : base( aspectLayerInstance, declaringNamespaceOrType, name, TypeKind.Delegate )
    {
        this.InvokeMethodBuilder = new MethodBuilder( aspectLayerInstance, this, _invokeMethodName ) { Accessibility = Accessibility.Public };
    }

    /// <summary>
    /// The base of a delegate is <see cref="System.MulticastDelegate"/>, which the code model reports and which the
    /// declaration does not write.
    /// </summary>
    protected override void InitializeBaseType()
    {
        this.BaseType = this.Compilation.Factory.GetSpecialType( InternalSpecialType.MulticastDelegate );
    }

    public IType ReturnType
    {
        get => this.InvokeMethodBuilder.ReturnType;
        set
        {
            this.CheckNotFrozen();

            this.InvokeMethodBuilder.ReturnType = value;
        }
    }

    public IParameterBuilder ReturnParameter => this.InvokeMethodBuilder.ReturnParameter;

    public IParameterBuilderList Parameters => this.InvokeMethodBuilder.Parameters;

    ITypeParameterList IDelegateBuilder.TypeParameters => this.TypeParameters;

    public IParameterBuilder AddParameter( string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = default )
    {
        this.CheckNotFrozen();

        return this.InvokeMethodBuilder.AddParameter( name, type, refKind, defaultValue );
    }

    public IParameterBuilder AddParameter( string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null )
    {
        this.CheckNotFrozen();

        return this.InvokeMethodBuilder.AddParameter( name, type, refKind, defaultValue );
    }

    /// <summary>
    /// Always <c>false</c>. The setter throws a <see cref="NotSupportedException"/>, because the language has no
    /// static delegate.
    /// </summary>
    public override bool IsStatic
    {
        get => false;
        set => throw NotSupported( nameof(this.IsStatic), "a delegate cannot be static" );
    }

    /// <summary>
    /// Always <c>false</c>. The setter throws a <see cref="NotSupportedException"/>, because the language has no
    /// abstract delegate.
    /// </summary>
    public override bool IsAbstract
    {
        get => false;
        set => throw NotSupported( nameof(this.IsAbstract), "a delegate cannot be abstract" );
    }

    /// <summary>
    /// Always <c>true</c>. The setter throws a <see cref="NotSupportedException"/>, because a delegate is
    /// implicitly sealed and the language refuses the modifier.
    /// </summary>
    public override bool IsSealed
    {
        get => true;
        set => throw NotSupported( nameof(this.IsSealed), "a delegate is implicitly sealed" );
    }

    /// <summary>
    /// Always <c>false</c>. The setter throws a <see cref="NotSupportedException"/>, because the language has no
    /// partial delegate.
    /// </summary>
    public override bool IsPartial
    {
        get => false;
        set => throw NotSupported( nameof(this.IsPartial), "the language has no partial delegate" );
    }

    /// <summary>
    /// Builds the exception that the setter of a property that is not valid for a delegate throws.
    /// </summary>
    private static NotSupportedException NotSupported( string propertyName, string reason )
        => new( $"The '{propertyName}' property cannot be set on a delegate builder, because {reason}." );

    protected override void FreezeChildren()
    {
        base.FreezeChildren();

        this.InvokeMethodBuilder.Freeze();
    }
}
