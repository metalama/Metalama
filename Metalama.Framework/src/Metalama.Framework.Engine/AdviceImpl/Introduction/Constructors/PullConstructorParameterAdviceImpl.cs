// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal class PullConstructorParameterAdviceImpl
{
    private readonly IPullStrategy? _pullStrategy;
    private readonly AspectLayerInstance _aspectLayerInstance;
    private readonly AdviceImplementationContext _context;

    public PullConstructorParameterAdviceImpl(
        AdviceImplementationContext context,
        IPullStrategy? pullStrategy,
        AspectLayerInstance aspectLayerInstance )
    {
        this._context = context;
        this._pullStrategy = pullStrategy;
        this._aspectLayerInstance = aspectLayerInstance;
    }

    private ProjectServiceProvider ServiceProvider => this._context.ServiceProvider;

    private CompilationModel Compilation => this._context.MutableCompilation;

    private void AddTransformation( ITransformation transformation ) => this._context.AddTransformation( transformation );

    public void PullConstructorParameterRecursive( IParameter baseParameter )
    {
        var baseConstructor = (IConstructor) baseParameter.DeclaringMember.AssertNotNull();
        var syntaxGenerationOptions = this.ServiceProvider.GetRequiredService<SyntaxGenerationOptions>();

        // Process the current type.
        var pulledParametersInCurrentType = new List<IParameter> { baseParameter };

        ProcessType(
            baseConstructor.DeclaringType.Constructors
                .Where( c => c.InitializerKind == ConstructorInitializerKind.This ),
            pulledParametersInCurrentType );

        // Register a transitive aspect for the current type.
        if ( pulledParametersInCurrentType.Count > 0 && this._pullStrategy is not null && this._pullStrategy is not LegacyPullStrategy )
        {
            var accessiblePulledParameters = pulledParametersInCurrentType.Where( c => c.DeclaringMember!.IsAccessibleFromOutsideAssembly() )
                .Select( p => p.ToRef() )
                .ToReadOnlyList();

            if ( accessiblePulledParameters.Count > 0 )
            {
                this._context.AddTransitiveAspect(
                    new IntroduceConstructorParameterTransitiveAspect( this._pullStrategy, accessiblePulledParameters, this._context.AspectOrder ) );
            }
        }

        // Process derived types.
        foreach ( var derivedType in this.Compilation.GetDerivedTypes( baseConstructor.DeclaringType, DerivedTypesOptions.DirectOnly ) )
        {
            ProcessType( derivedType.Constructors.Where( c => c.InitializerKind != ConstructorInitializerKind.This && !c.IsRecordCopyConstructor() ) );
        }

        return;

        void ProcessType( IEnumerable<IConstructor> potentialConstructors, List<IParameter>? pulledParameters = null )
        {
            var chainedConstructors =
                potentialConstructors.Where( c => ((IConstructorImpl) c).GetBaseConstructor()?.Definition.Equals( baseConstructor ) == true );

            // Process all of these constructors.
            foreach ( var chainedConstructor in chainedConstructors )
            {
                var chainedSyntaxGenerationContext = this.Compilation.CompilationContext.GetSyntaxGenerationContext(
                    syntaxGenerationOptions,
                    chainedConstructor.GetPrimaryDeclarationSyntax()
                    ?? chainedConstructor.DeclaringType.GetPrimaryDeclarationSyntax().AssertNotNull() );

                PullAction pullParameterAction;

                if ( this._pullStrategy != null )
                {
                    using ( UserCodeExecutionContext.WithContext(
                               this.ServiceProvider,
                               this.Compilation,
                               UserCodeDescription.Create( "evaluating the pull strategy for {0}", this ) ) )
                    {
                        // Ask the IPullStrategy what to do.
                        pullParameterAction = this._pullStrategy.GetPullAction( baseParameter, chainedConstructor );
                    }
                }
                else
                {
                    pullParameterAction = PullAction.None;
                }

                // If we have an implicit constructor, make it explicit.
                var initializedChainedConstructor = chainedConstructor;

                if ( chainedConstructor.IsImplicitInstanceConstructor() )
                {
                    var derivedConstructorBuilder = new ConstructorBuilder( this._aspectLayerInstance, chainedConstructor );

                    derivedConstructorBuilder.Freeze();
                    this.AddTransformation( derivedConstructorBuilder.CreateTransformation() );
                    initializedChainedConstructor = derivedConstructorBuilder;
                }

                // Execute the strategy.
                ExpressionSyntax parameterValue;

                switch ( pullParameterAction.Kind )
                {
                    case PullActionKind.DoNotPull:
                        // We do not add a new argument and reply on the optional value.
                        continue;

                    case PullActionKind.UseExpression:
                        parameterValue =
                            pullParameterAction.Expression.AssertNotNull()
                                .ToExpressionSyntax(
                                    new SyntaxSerializationContext(
                                        this.Compilation,
                                        chainedSyntaxGenerationContext,
                                        null,
                                        chainedConstructor.DeclaringType ) );

                        break;

                    case PullActionKind.AppendParameterAndPull:
                        // Create a new parameter.
                        parameterValue = SyntaxFactory.IdentifierName( pullParameterAction.ParameterName.AssertNotNull() );

                        TypedConstant? constant = null;

                        if ( pullParameterAction.ParameterDefaultValue != null
                             && !TypedConstant.TryConvertFromExpression( pullParameterAction.ParameterDefaultValue, out constant ) )
                        {
                            throw new AssertionFailedException( $"Cannot convert '{pullParameterAction.ParameterDefaultValue}' into a constant." );
                        }

                        var recursiveParameterBuilder = new ParameterBuilder(
                            initializedChainedConstructor,
                            initializedChainedConstructor.Parameters.Count,
                            pullParameterAction.ParameterName.AssertNotNull(),
                            pullParameterAction.ParameterType.AssertNotNull(),
                            RefKind.None,
                            this._aspectLayerInstance ) { DefaultValue = constant };

                        recursiveParameterBuilder.AddAttributes( pullParameterAction.ParameterAttributes );
                        recursiveParameterBuilder.Freeze();

                        this.AddTransformation( new IntroduceParameterTransformation( this._aspectLayerInstance, recursiveParameterBuilder.BuilderData ) );

                        var recursiveParameter = recursiveParameterBuilder;

                        // Process all constructors calling this constructor.
                        this.PullConstructorParameterRecursive( recursiveParameter );

                        pulledParameters?.Add( recursiveParameterBuilder );

                        break;

                    default:
                        throw new AssertionFailedException( $"Invalid value for PullActionKind: {pullParameterAction.Kind}." );
                }

                // Determine if we should qualify the argument with the parameter name.
                // We do this every time there is an optional parameter before the current parameter.
                var requiresParameterName = baseConstructor.Parameters.Any( p => p.DefaultValue != null && p.Index < baseParameter.Index );

                // Append an argument to the call to the current constructor. 
                this.AddTransformation(
                    new IntroduceConstructorInitializerArgumentTransformation(
                        this._aspectLayerInstance,
                        initializedChainedConstructor.ToFullRef(),
                        baseParameter.Index,
                        baseParameter.Name,
                        parameterValue,
                        requiresParameterName ) );
            }
        }
    }
}