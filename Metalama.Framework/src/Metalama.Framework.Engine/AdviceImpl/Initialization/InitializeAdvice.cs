// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

internal abstract class InitializeAdvice : Advice<AddInitializerAdviceResult>
{
    private readonly InitializerKind _kind;

    private new IMemberOrNamedType TargetDeclaration => (IMemberOrNamedType) base.TargetDeclaration;

    protected InitializeAdvice( in AdviceConstructorParameters<IMemberOrNamedType> parameters, InitializerKind kind ) : base( parameters )
    {
        this._kind = kind;
    }

    protected override AddInitializerAdviceResult Implement( AdviceImplementationContext context )
    {
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        var containingType = targetDeclaration.GetClosestNamedType().AssertNotNull();

        if ( targetDeclaration.DeclarationKind is DeclarationKind.NamedType or DeclarationKind.ExtensionBlock && containingType.IsRecord )
        {
            return this.CreateFailedResult(
                AdviceDiagnosticDescriptors.CannotAddInitializerToRecord.CreateRoslynDiagnostic(
                    containingType.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, containingType),
                    this ) );
        }

        IConstructor? staticConstructor;

        if ( this._kind == InitializerKind.BeforeTypeConstructor )
        {
            staticConstructor = containingType.StaticConstructor;

            if ( staticConstructor == null || staticConstructor.IsImplicitlyDeclared )
            {
                var staticConstructorBuilder =
                    new ConstructorBuilder( this.AspectLayerInstance, containingType ) { IsStatic = true, ReplacedImplicitConstructor = staticConstructor };

                staticConstructorBuilder.Freeze();
                staticConstructor = staticConstructorBuilder;
                context.AddTransformation( staticConstructorBuilder.CreateTransformation() );
            }
        }
        else
        {
            staticConstructor = null;
        }

        var constructors =
            targetDeclaration switch
            {
                IConstructor constructor => [constructor],
                INamedType => this._kind switch
                {
                    InitializerKind.BeforeTypeConstructor =>
                        [staticConstructor.AssertNotNull()],
                    InitializerKind.BeforeInstanceConstructor =>
                        containingType.Constructors
                            .Where( c => c.InitializerKind != ConstructorInitializerKind.This ),
                    _ => throw new AssertionFailedException( $"Unexpected initializer kind: {this._kind}." )
                },
                _ => throw new AssertionFailedException( $"Unexpected declaration: '{targetDeclaration}'." )
            };

        foreach ( var ctor in constructors )
        {
            IConstructor targetCtor;

            if ( ctor.IsImplicitInstanceConstructor() )
            {
                // Missing explicit ctor.
                var builder =
                    new ConstructorBuilder( this.AspectLayerInstance, ctor );

                builder.Freeze();
                context.AddTransformation( builder.CreateTransformation() );
                targetCtor = builder;
            }
            else
            {
                targetCtor = ctor;
            }

            this.AddTransformation( targetDeclaration, targetCtor, context.AddTransformation );
        }

        return new AddInitializerAdviceResult( AdviceOutcome.Success, this.AdviceFactory );
    }

    protected abstract void AddTransformation( IMemberOrNamedType targetDeclaration, IConstructor targetCtor, Action<ITransformation> addTransformation );

    public override AdviceKind AdviceKind => AdviceKind.AddInitializer;

    protected override AddInitializerAdviceResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceOutcome.Error, this.AdviceFactory, diagnostics );
}