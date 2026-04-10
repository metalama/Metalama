// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.RunTime;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

/// <summary>
/// Helper that implements the "forwarding constructor" side-effect of <c>IntroduceParameter</c>.
/// When the current advice mutates a constructor and the caller-supplied <see cref="IConstructorOverloadingStrategy"/>
/// asks for a forwarder, this helper creates (or extends) a compile-time stub that keeps the pre-mutation
/// signature of the constructor callable and forwards a strategy-supplied expression as the value of the
/// newly introduced parameter.
/// </summary>
internal sealed class ForwardingConstructorHelper
{
    private readonly AdviceImplementationContext _context;
    private readonly AspectLayerInstance _aspectLayerInstance;
    private readonly IConstructorOverloadingStrategy? _overloadingStrategy;
    private readonly IPullStrategy? _pullStrategy;
    private readonly Advice _owningAdvice;

    public ForwardingConstructorHelper(
        AdviceImplementationContext context,
        AspectLayerInstance aspectLayerInstance,
        IConstructorOverloadingStrategy? overloadingStrategy,
        IPullStrategy? pullStrategy,
        Advice owningAdvice )
    {
        this._context = context;
        this._aspectLayerInstance = aspectLayerInstance;
        this._overloadingStrategy = overloadingStrategy;
        this._pullStrategy = pullStrategy;
        this._owningAdvice = owningAdvice;
    }

    public IConstructorOverloadingStrategy? OverloadingStrategy => this._overloadingStrategy;

    public bool IsEnabled => this._overloadingStrategy is not null;

    /// <summary>
    /// Invoked after <paramref name="mutatedConstructor"/> has received <paramref name="introducedParameter"/>.
    /// If the overloading strategy asks for a forwarder, this method creates or extends one.
    /// </summary>
    public void ApplyForwarderIfNeeded( IConstructor mutatedConstructor, IParameter introducedParameter )
    {
        if ( !this.IsEnabled || this._pullStrategy is null )
        {
            return;
        }

        if ( !this.ShouldGenerateForwarder( mutatedConstructor, introducedParameter ) )
        {
            return;
        }

        // Compute pre-mutation parameters: the ones originating from source (aspect-introduced parameters
        // are excluded).
        var preMutationParams = mutatedConstructor.Parameters
            .Where( p => p.Origin.Kind == DeclarationOriginKind.Source )
            .ToList();

        // Find an existing forwarder for this constructor (from an earlier advice invocation).
        var existingForwarder = this.FindExistingForwarder( mutatedConstructor, preMutationParams );

        if ( existingForwarder is not null )
        {
            this.ExtendForwarder( existingForwarder, introducedParameter );
        }
        else
        {
            this.CreateForwarder( mutatedConstructor, preMutationParams, introducedParameter );
        }
    }

    private bool ShouldGenerateForwarder( IConstructor mutatedConstructor, IParameter introducedParameter )
    {
        using ( UserCodeExecutionContext.WithContext(
                   this._context.ServiceProvider,
                   this._context.MutableCompilation,
                   UserCodeDescription.Create( "evaluating the constructor overloading strategy" ) ) )
        {
            return this._overloadingStrategy!.ShouldGenerateForwarder( mutatedConstructor, introducedParameter );
        }
    }

    private IConstructor? FindExistingForwarder( IConstructor mutatedConstructor, IReadOnlyList<IParameter> preMutationParams )
    {
        var comparer = this._context.MutableCompilation.Comparers.Default;

        foreach ( var ctor in mutatedConstructor.DeclaringType.Constructors )
        {
            if ( !ctor.IsAspectGeneratedForwarder() )
            {
                continue;
            }

            if ( ctor.Parameters.Count != preMutationParams.Count )
            {
                continue;
            }

            var match = true;

            for ( var i = 0; i < preMutationParams.Count; i++ )
            {
                if ( !comparer.Equals( ctor.Parameters[i].Type, preMutationParams[i].Type )
                     || ctor.Parameters[i].RefKind != preMutationParams[i].RefKind )
                {
                    match = false;

                    break;
                }
            }

            if ( match )
            {
                return ctor;
            }
        }

        return null;
    }

    private void CreateForwarder(
        IConstructor mutatedConstructor,
        IReadOnlyList<IParameter> preMutationParams,
        IParameter introducedParameter )
    {
        var forwarderBuilder = new ConstructorBuilder( this._aspectLayerInstance, mutatedConstructor.DeclaringType )
        {
            Accessibility = mutatedConstructor.Accessibility,
            IsStatic = false,
            InitializerKind = ConstructorInitializerKind.This,

            // The original source constructor is still physically present in the user's source at design time,
            // so the source generator must not emit the forwarding constructor (it would collide). The forwarder
            // is only needed at compile time, when the source constructor has been mutated with the introduced parameter.
            IsDesignTimeObservableOverride = false
        };

        // Add the marker attribute up-front so the user's pull strategy can detect the forwarder via
        // IsAspectGeneratedForwarder() when GetForwardingExpression calls it below.
        forwarderBuilder.AddAttribute( AttributeConstruction.Create( typeof(AspectGeneratedForwardingConstructorAttribute) ) );

        // Copy the pre-mutation parameter list (source-origin parameters only).
        foreach ( var sourceParameter in preMutationParams )
        {
            forwarderBuilder.AddParameter(
                sourceParameter.Name,
                sourceParameter.Type,
                sourceParameter.RefKind,
                sourceParameter.DefaultValue );
        }

        // Forward the pre-mutation parameters by name to the :this(...) call.
        foreach ( var sourceParameter in preMutationParams )
        {
            forwarderBuilder.AddInitializerArgument(
                new SyntaxUserExpression(
                    SyntaxFactoryEx.SafeIdentifierName( sourceParameter.Name ),
                    sourceParameter.Type ),
                sourceParameter.Name );
        }

        // Forward any aspect-introduced parameters that predate the current advice (possibly from a
        // prior advice that did not forward). These are visible in the mutable compilation. We
        // exclude the newly introduced parameter here because it is not guaranteed to be reflected
        // in the mutable compilation yet — advice transformations are not flushed until the advice
        // returns. We forward it explicitly afterwards.
        foreach ( var aspectParameter in mutatedConstructor.Parameters
                     .Where( p => p.Origin.Kind == DeclarationOriginKind.Aspect && p.Name != introducedParameter.Name ) )
        {
            var priorForwardingExpression = this.GetForwardingExpression( aspectParameter, forwarderBuilder );

            if ( priorForwardingExpression is null )
            {
                return;
            }

            forwarderBuilder.AddInitializerArgument( priorForwardingExpression, aspectParameter.Name );
        }

        // Forward the newly introduced parameter.
        var forwardingExpression = this.GetForwardingExpression( introducedParameter, forwarderBuilder );

        if ( forwardingExpression is null )
        {
            return;
        }

        forwarderBuilder.AddInitializerArgument( forwardingExpression, introducedParameter.Name );

        forwarderBuilder.Freeze();
        this._context.AddTransformation( forwarderBuilder.CreateTransformation() );
    }

    private void ExtendForwarder( IConstructor existingForwarder, IParameter introducedParameter )
    {
        // Ask the strategy for the forwarding expression for the newly introduced parameter.
        var forwardingExpression = this.GetForwardingExpression( introducedParameter, existingForwarder );

        if ( forwardingExpression is null )
        {
            return;
        }

        // Convert the expression to a syntax node for the argument. Use the declaring type's primary
        // declaration syntax for the generation context — the forwarder itself is a builder with no syntax.
        var syntaxGenerationOptions = this._context.ServiceProvider.GetRequiredService<SyntaxGenerationOptions>();
        var forwarderSyntaxGenerationContext = this._context.MutableCompilation.CompilationContext.GetSyntaxGenerationContext(
            syntaxGenerationOptions,
            existingForwarder.DeclaringType.GetPrimaryDeclarationSyntax().AssertNotNull() );

        var expressionSyntax = forwardingExpression.ToExpressionSyntax(
            new SyntaxSerializationContext(
                this._context.MutableCompilation,
                forwarderSyntaxGenerationContext,
                null,
                existingForwarder.DeclaringType ) );

        this._context.AddTransformation(
            new IntroduceConstructorInitializerArgumentTransformation(
                this._aspectLayerInstance,
                existingForwarder.ToFullRef(),
                introducedParameter.Index,
                introducedParameter.Name,
                expressionSyntax,
                requiresParameterName: true ) );
    }

    private IExpression? GetForwardingExpression( IParameter introducedParameter, IConstructor forwarderTarget )
    {
        PullAction pullAction;

        using ( UserCodeExecutionContext.WithContext(
                   this._context.ServiceProvider,
                   this._context.MutableCompilation,
                   UserCodeDescription.Create( "evaluating the pull strategy for an aspect-generated forwarding constructor" ) ) )
        {
            pullAction = this._pullStrategy!.GetPullAction( introducedParameter, forwarderTarget );
        }

        switch ( pullAction.Kind )
        {
            case PullActionKind.UseExpression:
                return pullAction.Expression;

            default:
                // Only UseExpression / UseConstant / UseExistingParameter are valid for forwarders —
                // they all map to UseExpression. DoNotPull and AppendParameterAndPull are invalid.
                this._context.Diagnostics.Report(
                    AdviceDiagnosticDescriptors.InvalidPullActionForAspectGeneratedForwarder.CreateRoslynDiagnostic(
                        forwarderTarget.GetDiagnosticLocation(),
                        (this._owningAdvice.AspectInstance.AspectClass.ShortName, introducedParameter, forwarderTarget,
                         pullAction.Kind.ToString()),
                        this._owningAdvice ) );

                return null;
        }
    }
}
