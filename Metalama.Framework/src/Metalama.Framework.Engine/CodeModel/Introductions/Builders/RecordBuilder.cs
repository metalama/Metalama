// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using System;
using System.Collections.Generic;
using RecordKind = Metalama.Framework.Code.RecordKind;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Builds a record that an advice introduces, in either of its two authoring forms.
/// </summary>
/// <remarks>
/// <para>
/// Metalama emits the record declaration, which is the <c>record</c> keyword, the name, the positional parameter
/// list and the base list, and the compiler synthesizes the members from it exactly as it does for a record the
/// user wrote. Those members must nevertheless exist in the code model, because the pipeline never re-reads the
/// final model from Roslyn, so this class creates a builder for each of them and the advice registers it without
/// emitting it. Section 4.2 of <c>Metalama.Framework/docs/future/introducing-types.md</c> states the rule.
/// </para>
/// <para>
/// The members are created when the builder is frozen rather than in the constructor, because the positional
/// parameters decide several of them and an aspect adds those at any point in the callback.
/// </para>
/// </remarks>
internal sealed class RecordBuilder : NamedTypeBuilder, IRecordBuilder
{
    /// <summary>
    /// The name that the compiler gives to the property carrying the equality contract of a record class.
    /// </summary>
    private const string _equalityContractPropertyName = "EqualityContract";

    /// <summary>
    /// The name that the compiler gives to the method printing the members of a record.
    /// </summary>
    private const string _printMembersMethodName = "PrintMembers";

    /// <summary>
    /// The metadata name of the clone method of a record class. It is not a C# identifier, so the method is absent
    /// from <see cref="INamedType.Methods"/> and is reached through the facet alone.
    /// </summary>
    private const string _cloneMethodMetadataName = "<Clone>$";

    /// <summary>
    /// The name that the compiler gives to the method deconstructing a positional record.
    /// </summary>
    private const string _deconstructMethodName = "Deconstruct";

    private readonly ConstructorBuilder _primaryConstructor;
    private readonly List<PropertyBuilder> _positionalProperties = [];

    // The three lists are typed, because each builder class declares its own BuilderData property and no common
    // base declares one.
    private readonly List<PropertyBuilder> _synthesizedProperties = [];
    private readonly List<MethodBuilder> _synthesizedMethods = [];
    private readonly List<ConstructorBuilder> _synthesizedConstructors = [];

    public RecordBuilder(
        AspectLayerInstance aspectLayerInstance,
        INamespaceOrNamedType declaringNamespaceOrType,
        string name,
        RecordKind recordKind )
        : base(
            aspectLayerInstance,
            declaringNamespaceOrType,
            name,
            recordKind == RecordKind.Struct ? TypeKind.Struct : TypeKind.Class,
            isRecord: true )
    {
        this.RecordKind = recordKind;

        this._primaryConstructor = new ConstructorBuilder( aspectLayerInstance, this )
        {
            Accessibility = Accessibility.Public,
            IsPrimary = true
        };
    }

    public RecordKind RecordKind { get; }

    /// <summary>
    /// Gets a value indicating whether the record is a reference type, which decides the members that the compiler
    /// synthesizes for it.
    /// </summary>
    private bool IsRecordClass => this.RecordKind == RecordKind.Class;

    /// <summary>
    /// Gets the primary constructor, whose parameters are the positional parameters of the record.
    /// </summary>
    public ConstructorBuilder PrimaryConstructorBuilder => this._primaryConstructor;

    /// <summary>
    /// Gets the immutable data of every member that the compiler synthesizes and that the advice registers in the
    /// code model without emitting it. The builder must be frozen before this method is called, because the data of
    /// a builder exists only from that point.
    /// </summary>
    public IEnumerable<NamedDeclarationBuilderData> GetSynthesizedMemberData()
    {
        foreach ( var constructor in this._synthesizedConstructors )
        {
            yield return constructor.BuilderData;
        }

        foreach ( var property in this._synthesizedProperties )
        {
            yield return property.BuilderData;
        }

        foreach ( var method in this._synthesizedMethods )
        {
            // The name of the clone method is not a C# identifier, so INamedType.Methods does not contain it for a
            // record read from source, and registering it would make an introduced record diverge. The facet
            // resolves it from the builder data instead, which needs no transformation.
            if ( ReferenceEquals( method, this.CloneMethod ) )
            {
                continue;
            }

            yield return method.BuilderData;
        }
    }

    public IParameterBuilderList PositionalParameters => this._primaryConstructor.Parameters;

    public IParameterBuilder AddPositionalParameter( string name, IType type, TypedConstant? defaultValue = default )
    {
        this.CheckNotFrozen();

        return this._primaryConstructor.AddParameter( name, type, RefKind.None, defaultValue );
    }

    public IParameterBuilder AddPositionalParameter( string name, Type type, TypedConstant? defaultValue = null )
    {
        this.CheckNotFrozen();

        return this._primaryConstructor.AddParameter( name, type, RefKind.None, defaultValue );
    }

    /// <summary>
    /// Gets the property that a positional parameter declares, in the order in which the parameters were added.
    /// </summary>
    public IReadOnlyList<PropertyBuilder> PositionalPropertyBuilders => this._positionalProperties;

    /// <summary>
    /// Gets the property carrying the equality contract, or <c>null</c> for a record struct, which has none.
    /// </summary>
    public PropertyBuilder? EqualityContractProperty { get; private set; }

    /// <summary>
    /// Gets the method printing the members of the record.
    /// </summary>
    public MethodBuilder? PrintMembersMethod { get; private set; }

    /// <summary>
    /// Gets the clone method, or <c>null</c> for a record struct, which has none.
    /// </summary>
    public MethodBuilder? CloneMethod { get; private set; }

    /// <summary>
    /// Gets the copy constructor, or <c>null</c> for a record struct, which has none.
    /// </summary>
    public ConstructorBuilder? CopyConstructor { get; private set; }

    /// <summary>
    /// Gets the deconstructing method, or <c>null</c> when the record declares no positional parameter.
    /// </summary>
    public MethodBuilder? DeconstructMethod { get; private set; }

    protected override void FreezeChildren()
    {
        this.MaterializeSynthesizedMembers();

        base.FreezeChildren();

        foreach ( var constructor in this._synthesizedConstructors )
        {
            constructor.Freeze();
        }

        foreach ( var property in this._synthesizedProperties )
        {
            property.Freeze();
        }

        foreach ( var method in this._synthesizedMethods )
        {
            method.Freeze();
        }
    }

    /// <summary>
    /// Creates a builder for every member that the compiler synthesizes from the record declaration.
    /// </summary>
    private void MaterializeSynthesizedMembers()
    {
        this._synthesizedConstructors.Add( this._primaryConstructor );

        this.MaterializePositionalProperties();
        this.MaterializeEqualityContractProperty();
        this.MaterializePrintMembersMethod();
        this.MaterializeCloneMethodAndCopyConstructor();
        this.MaterializeDeconstructMethod();
        this.MaterializeEqualityMembers();
    }

    /// <summary>
    /// Creates the public init-only property that each positional parameter declares.
    /// </summary>
    private void MaterializePositionalProperties()
    {
        foreach ( var parameter in this._primaryConstructor.Parameters )
        {
            var property = new PropertyBuilder(
                this.AspectLayerInstance,
                this,
                parameter.Name,
                hasGetter: true,
                hasSetter: true,
                isAutoProperty: true,
                hasInitOnlySetter: true,
                hasImplicitGetter: true,
                hasImplicitSetter: true )
            {
                Type = parameter.Type,
                Accessibility = Accessibility.Public
            };

            this._positionalProperties.Add( property );
            this._synthesizedProperties.Add( property );
        }
    }

    /// <summary>
    /// Creates the property carrying the equality contract, which a record class has and a record struct does not.
    /// </summary>
    private void MaterializeEqualityContractProperty()
    {
        if ( !this.IsRecordClass )
        {
            return;
        }

        var property = new PropertyBuilder(
            this.AspectLayerInstance,
            this,
            _equalityContractPropertyName,
            hasGetter: true,
            hasSetter: false,
            isAutoProperty: false,
            hasInitOnlySetter: false,
            hasImplicitGetter: false,
            hasImplicitSetter: false )
        {
            Type = this.Compilation.Factory.GetTypeByReflectionName( "System.Type" ),
            Accessibility = this.IsSealed ? Accessibility.Private : Accessibility.Protected,
            IsVirtual = !this.IsSealed
        };

        this.EqualityContractProperty = property;
        this._synthesizedProperties.Add( property );
    }

    /// <summary>
    /// Creates the method printing the members of the record, which is <c>protected virtual</c> on a record class
    /// that is not sealed and <c>private</c> on one that is, and always <c>private</c> on a record struct.
    /// </summary>
    private void MaterializePrintMembersMethod()
    {
        var isPrivate = !this.IsRecordClass || this.IsSealed;

        var method = new MethodBuilder( this.AspectLayerInstance, this, _printMembersMethodName )
        {
            Accessibility = isPrivate ? Accessibility.Private : Accessibility.Protected,
            IsVirtual = !isPrivate,
            ReturnType = this.Compilation.Factory.GetSpecialType( SpecialType.Boolean )
        };

        method.AddParameter( "builder", this.Compilation.Factory.GetTypeByReflectionName( "System.Text.StringBuilder" ) );

        this.PrintMembersMethod = method;
        this._synthesizedMethods.Add( method );
    }

    /// <summary>
    /// Creates the clone method and the copy constructor, which a record class has and a record struct does not.
    /// </summary>
    private void MaterializeCloneMethodAndCopyConstructor()
    {
        if ( !this.IsRecordClass )
        {
            return;
        }

        var cloneMethod = new MethodBuilder( this.AspectLayerInstance, this, _cloneMethodMetadataName )
        {
            Accessibility = Accessibility.Public,
            IsVirtual = !this.IsSealed,
            ReturnType = this
        };

        this.CloneMethod = cloneMethod;
        this._synthesizedMethods.Add( cloneMethod );

        var copyConstructor = new ConstructorBuilder( this.AspectLayerInstance, this )
        {
            Accessibility = this.IsSealed ? Accessibility.Private : Accessibility.Protected
        };

        copyConstructor.AddParameter( "original", this );

        this.CopyConstructor = copyConstructor;
        this._synthesizedConstructors.Add( copyConstructor );
    }

    /// <summary>
    /// Creates the deconstructing method, which the compiler synthesizes for a record that declares at least one
    /// positional parameter.
    /// </summary>
    private void MaterializeDeconstructMethod()
    {
        if ( this._primaryConstructor.Parameters.Count == 0 )
        {
            return;
        }

        var method = new MethodBuilder( this.AspectLayerInstance, this, _deconstructMethodName )
        {
            Accessibility = Accessibility.Public,
            ReturnType = this.Compilation.Factory.GetSpecialType( SpecialType.Void )
        };

        foreach ( var parameter in this._primaryConstructor.Parameters )
        {
            method.AddParameter( parameter.Name, parameter.Type, RefKind.Out );
        }

        this.DeconstructMethod = method;
        this._synthesizedMethods.Add( method );
    }

    /// <summary>
    /// Creates the members implementing equality, which are not part of the facet and which
    /// <see cref="INamedType.Methods"/> nevertheless has to report.
    /// </summary>
    private void MaterializeEqualityMembers()
    {
        var booleanType = this.Compilation.Factory.GetSpecialType( SpecialType.Boolean );
        var objectType = this.Compilation.Factory.GetSpecialType( SpecialType.Object );

        var equalsObject = new MethodBuilder( this.AspectLayerInstance, this, nameof(this.Equals) )
        {
            Accessibility = Accessibility.Public,
            IsOverride = true,
            ReturnType = booleanType
        };

        // The parameters below are reported without their nullable annotation. A builder cannot produce the nullable
        // form of the type it is building, because NamedTypeBuilder.ToNullable is not implemented, and the annotation
        // reaches neither the emitted code nor any member that the facet names.
        equalsObject.AddParameter( "obj", objectType );
        this._synthesizedMethods.Add( equalsObject );

        var equalsTyped = new MethodBuilder( this.AspectLayerInstance, this, nameof(this.Equals) )
        {
            Accessibility = Accessibility.Public,
            IsVirtual = this.IsRecordClass && !this.IsSealed,
            ReturnType = booleanType
        };

        equalsTyped.AddParameter( "other", this );
        this._synthesizedMethods.Add( equalsTyped );

        var getHashCode = new MethodBuilder( this.AspectLayerInstance, this, nameof(this.GetHashCode) )
        {
            Accessibility = Accessibility.Public,
            IsOverride = true,
            ReturnType = this.Compilation.Factory.GetSpecialType( SpecialType.Int32 )
        };

        this._synthesizedMethods.Add( getHashCode );

        var toString = new MethodBuilder( this.AspectLayerInstance, this, nameof(this.ToString) )
        {
            Accessibility = Accessibility.Public,
            IsOverride = true,
            ReturnType = this.Compilation.Factory.GetSpecialType( SpecialType.String )
        };

        this._synthesizedMethods.Add( toString );

        this._synthesizedMethods.Add( this.CreateEqualityOperator( OperatorKind.Equality, booleanType ) );
        this._synthesizedMethods.Add( this.CreateEqualityOperator( OperatorKind.Inequality, booleanType ) );
    }

    /// <summary>
    /// Creates one of the two equality operators that the compiler synthesizes for a record.
    /// </summary>
    private MethodBuilder CreateEqualityOperator( OperatorKind operatorKind, IType booleanType )
    {
        // The setter of OperatorKind gives the method the metadata name and the static modifier that the operator
        // requires, which the constructor does not, so the kind is set after construction.
        var method = new MethodBuilder( this.AspectLayerInstance, this, "op" )
        {
            OperatorKind = operatorKind,
            Accessibility = Accessibility.Public,
            ReturnType = booleanType
        };

        method.AddParameter( "left", this );
        method.AddParameter( "right", this );

        return method;
    }
}
