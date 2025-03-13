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

    public override AdviceKind AdviceKind => AdviceKind.IntroduceType;

    private OverrideStrategy OverrideStrategy { get; }

    public IntroduceNamedTypeAdvice(
        AdviceConstructorParameters<INamespaceOrNamedType> parameters,
        string explicitName,
        OverrideStrategy overrideStrategy,
        Action<NamedTypeBuilder>? buildAction,
        TypeKind typeKind,
        IAdviceFactoryImpl adviceFactory )
        : base( parameters, buildAction, adviceFactory )
    {
        this._explicitName = explicitName;
        this.OverrideStrategy = overrideStrategy;
        this._typeKind = typeKind;
    }

    protected override NamedTypeBuilder CreateBuilder()
    {
        return new NamedTypeBuilder(
            this.AspectLayerInstance,
            (INamespaceOrNamedType) this.TargetDeclaration.AssertNotNull(),
            this._explicitName,
            this._typeKind );
    }

    protected override IntroductionAdviceResult<INamedType> ImplementCore( NamedTypeBuilder builder, in AdviceImplementationContext context )
    {
        var targetDeclaration = (INamespaceOrNamedType) this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        var existingType =
            targetDeclaration switch
            {
                INamespace @namespace =>
                    @namespace.Types
                        .OfName( builder.Name )
                        .FirstOrDefault( t => builder.TypeParameters.Count == t.TypeParameters.Count ),
                INamedType namedType =>
                    namedType.AllTypes
                        .OfName( builder.Name )
                        .FirstOrDefault( t => builder.TypeParameters.Count == t.TypeParameters.Count ),
                _ => throw new AssertionFailedException( $"Unsupported: {targetDeclaration}" )
            };

        if ( existingType == null )
        {
            builder.Freeze();

            context.AddTransformation( builder.CreateTransformation() );

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

                    return this.CreateSuccessResult( AdviceOutcome.Default, builder );

                default:
                    throw new AssertionFailedException( $"Unexpected OverrideStrategy: {this.OverrideStrategy}." );
            }
        }
    }
}