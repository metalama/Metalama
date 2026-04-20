// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// Cross-project counterpart to <see cref="OnConstructedMethodAdvice"/>: walks the derived types of
/// <see cref="Advice.TargetDeclaration"/> and emits, for each non-<c>:this(...)</c> instance constructor,
/// the <c>OnConstructed</c> epilogue and the <c>:base(context.Descend(OnConstructed))</c> rewrite.
/// Invoked from <see cref="AddConstructorEpilogueTransitiveAspect"/> in dependent projects after
/// <see cref="Introduction.Constructors.PullConstructorParameterTransitiveAspect"/> has run, so the
/// <c>InitializationContext</c> parameter is expected to already be present on the derived constructors.
/// </summary>
internal sealed class OnConstructedEpilogueAdvice : Advice<EmptyAdviceResult>
{
    public OnConstructedEpilogueAdvice( in AdviceConstructorParameters<INamedType> parameters )
        : base( parameters ) { }

    protected override bool AcceptsExternalTargets => true;

    public override AdviceKind AdviceKind => AdviceKind.AddInitializer;

    protected override EmptyAdviceResult Implement( AdviceImplementationContext context )
    {
        var baseType = ((INamedType) this.TargetDeclaration).ToRef().GetTarget( context.MutableCompilation );
        var initContextType = baseType.Compilation.Factory.GetTypeByReflectionType( typeof(InitializationContext) );

        foreach ( var derivedType in baseType.Compilation.GetDerivedTypes( baseType, DerivedTypesOptions.All ) )
        {
            // The emitter filters out the compiler-generated copy constructor per-ctor, so record
            // primary / explicit / user-authored constructors receive the epilogue just like classes.
            OnConstructedEpilogueEmitter.EmitForType(
                derivedType,
                initContextType,
                this.AspectLayerInstance,
                context,
                registerPullFallback: false );
        }

        return new EmptyAdviceResult( AdviceKind.AddInitializer, AdviceOutcome.Success, this.AdviceFactory );
    }

    protected override EmptyAdviceResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceKind.AddInitializer, AdviceOutcome.Error, this.AdviceFactory, diagnostics );
}