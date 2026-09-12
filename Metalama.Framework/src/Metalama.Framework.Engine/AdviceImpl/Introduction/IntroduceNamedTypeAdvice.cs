// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.Diagnostics;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceNamedTypeAdvice : IntroduceDeclarationAdvice<INamedType, NamedTypeBuilder>
{
    private readonly string _explicitName;
    private readonly TypeKind _typeKind;

    /// <summary>
    /// The authoring form of a record, or <see cref="RecordKind.None"/> when the introduced type is not a record.
    /// </summary>
    private readonly RecordKind _recordKind;

    /// <summary>
    /// A value indicating whether the introduced type is a union written with the <c>union</c> keyword, which the
    /// language reports as a struct.
    /// </summary>
    private readonly bool _isUnion;

    public override AdviceKind AdviceKind => AdviceKind.IntroduceType;

    private OverrideStrategy OverrideStrategy { get; }

    public IntroduceNamedTypeAdvice(
        in AdviceConstructorParameters<INamespaceOrNamedType> parameters,
        string explicitName,
        OverrideStrategy overrideStrategy,
        Action<NamedTypeBuilder>? buildAction,
        TypeKind typeKind,
        RecordKind recordKind = RecordKind.None,
        bool isUnion = false )
        : base( parameters, buildAction )
    {
        this._explicitName = explicitName;
        this.OverrideStrategy = overrideStrategy;
        this._typeKind = typeKind;
        this._recordKind = recordKind;
        this._isUnion = isUnion;
    }

    protected override NamedTypeBuilder CreateBuilder()
    {
        var target = (INamespaceOrNamedType) this.TargetDeclaration.AssertNotNull();

        // Each kind whose builder carries state of its own has a class of its own. The compilation model requires an
        // INamedTypeImpl in every case, so each of them derives from NamedTypeBuilder and narrows what it exposes.
        if ( this._isUnion )
        {
            return new UnionBuilder( this.AspectLayerInstance, target, this._explicitName );
        }
        else if ( this._recordKind != RecordKind.None )
        {
            return new RecordBuilder( this.AspectLayerInstance, target, this._explicitName, this._recordKind );
        }
        else
        {
            return this._typeKind switch
            {
                TypeKind.Enum => new EnumBuilder( this.AspectLayerInstance, target, this._explicitName ),
                TypeKind.Delegate => new DelegateBuilder( this.AspectLayerInstance, target, this._explicitName ),
                _ => new NamedTypeBuilder( this.AspectLayerInstance, target, this._explicitName, this._typeKind )
            };
        }
    }

    protected override IntroductionAdviceResult<INamedType> ImplementCore( NamedTypeBuilder builder, AdviceImplementationContext context )
    {
        var targetDeclaration = (INamespaceOrNamedType) this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        var existingType =
            targetDeclaration.DeclarationKind switch
            {
                DeclarationKind.Namespace when targetDeclaration is INamespace @namespace =>
                    @namespace.Types
                        .OfName( builder.Name )
                        .FirstOrDefault( t => builder.TypeParameters.Count == t.TypeParameters.Count ),
                DeclarationKind.NamedType when targetDeclaration is INamedType namedType =>
                    namedType.AllTypes
                        .OfName( builder.Name )
                        .FirstOrDefault( t => builder.TypeParameters.Count == t.TypeParameters.Count ),
                _ => throw new AssertionFailedException( $"Unsupported: {targetDeclaration}" )
            };

        if ( existingType == null )
        {
            builder.Freeze();

            context.AddTransformation( builder.CreateTransformation() );

            this.RegisterOwnedMembers( builder, context );
            this.IntroduceImplicitConstructorIfNeeded( builder, context );

            return this.CreateSuccessResult( AdviceOutcome.Default, builder );
        }
        else
        {
            switch ( this.OverrideStrategy )
            {
                case OverrideStrategy.Fail:
                    return this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.CannotIntroduceNewTypeWhenItAlreadyExists.CreateRoslynDiagnostic(
                            targetDeclaration.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration),
                            this ) );

                case OverrideStrategy.Ignore:
                    return this.CreateIgnoredResult( existingType );

                case OverrideStrategy.New:
                    builder.HasNewKeyword = builder.IsNew = true;
                    builder.Freeze();
                    context.AddTransformation( builder.CreateTransformation() );

                    this.RegisterOwnedMembers( builder, context );
                    this.IntroduceImplicitConstructorIfNeeded( builder, context );

                    return this.CreateSuccessResult( AdviceOutcome.Default, builder );

                default:
                    throw new AssertionFailedException( $"Unexpected OverrideStrategy: {this.OverrideStrategy}." );
            }
        }
    }

    /// <summary>
    /// Registers in the code model the members that the declaration of the introduced type carries and that nothing
    /// emits, which is section 4.2 of <c>Metalama.Framework/docs/introducing-types.md</c>.
    /// </summary>
    private void RegisterOwnedMembers( NamedTypeBuilder builder, AdviceImplementationContext context )
    {
        if ( builder is ITypeBuilderWithSynthesizedMembers builderWithSynthesizedMembers )
        {
            foreach ( var member in builderWithSynthesizedMembers.GetSynthesizedMemberData() )
            {
                context.AddTransformation( new IntroduceSynthesizedDeclarationTransformation( this.AspectLayerInstance, member ) );
            }
        }
    }

    private void IntroduceImplicitConstructorIfNeeded( NamedTypeBuilder builder, AdviceImplementationContext context )
    {
        // A non-static class and a struct both have an implicit parameterless constructor, just like a source type
        // that gets one from Roslyn. The pipeline never re-reads the final model from Roslyn, so the constructor has
        // to exist as a builder for an aspect to see it.
        if ( builder is not { TypeKind: TypeKind.Class or TypeKind.Struct, IsStatic: false } )
        {
            return;
        }

        var constructorBuilder = new ConstructorBuilder( this.AspectLayerInstance, builder, isImplicitlyDeclared: true )
        {
            Accessibility = Accessibility.Public
        };

        constructorBuilder.Freeze();

        if ( builder.TypeKind == TypeKind.Class )
        {
            // Metalama declares the parameterless constructor of a class, so it is registered and emitted.
            context.AddTransformation( constructorBuilder.CreateTransformation() );
        }
        else
        {
            // The compiler synthesizes the parameterless constructor of a struct from the declaration, so this one is
            // registered in the code model and emitted by nothing. Emitting it as well would declare it twice, and
            // before C# 10 the language did not let a struct declare one at all. See section 4.2 of
            // Metalama.Framework/docs/introducing-types.md.
            context.AddTransformation(
                new IntroduceSynthesizedDeclarationTransformation( this.AspectLayerInstance, constructorBuilder.BuilderData ) );
        }
    }
}