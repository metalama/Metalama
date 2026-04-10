// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed class PullConstructorParameterAdviceImpl
{
    private readonly IPullStrategy? _pullStrategy;
    private readonly AspectLayerInstance _aspectLayerInstance;
    private readonly bool _onlyProcessDerivedTypes;
    private readonly AdviceImplementationContext _context;
    private readonly ForwardingConstructorHelper? _forwardingHelper;

    public PullConstructorParameterAdviceImpl(
        AdviceImplementationContext context,
        IPullStrategy? pullStrategy,
        AspectLayerInstance aspectLayerInstance,
        bool onlyProcessDerivedTypes,
        ForwardingConstructorHelper? forwardingHelper = null )
    {
        this._context = context;
        this._pullStrategy = pullStrategy;
        this._aspectLayerInstance = aspectLayerInstance;
        this._onlyProcessDerivedTypes = onlyProcessDerivedTypes;
        this._forwardingHelper = forwardingHelper;
    }

    private ProjectServiceProvider ServiceProvider => this._context.ServiceProvider;

    private CompilationModel Compilation => this._context.MutableCompilation;

    private void AddTransformation( ITransformation transformation ) => this._context.AddTransformation( transformation );

    /// <summary>
    /// Determines whether <paramref name="child"/>'s <c>: base(...)</c>/<c>: this(...)</c> call targets <paramref name="target"/>,
    /// either directly or through a source-compatibility constructor. When the base constructor lives in a
    /// referenced project, Roslyn may resolve the chain call to a forwarder emitted into the dependency's IL (arity match)
    /// rather than the mutated constructor; we "see through" such forwarders by matching their parameter prefix against
    /// <paramref name="target"/>'s parameters, since the framework only ever appends new parameters to mutated constructors.
    /// </summary>
    private static bool IsChainedCall( IConstructor child, IConstructor target )
    {
        var resolved = ((IConstructorImpl) child).GetBaseConstructor()?.Definition;

        if ( resolved is null )
        {
            return false;
        }

        if ( resolved.Equals( target ) )
        {
            return true;
        }

        // Roslyn's SemanticModel resolves `: base(id)` (or `: this(id)`) to whichever
        // ctor matches the source arity in IL. In cross-project scenarios that means
        // it resolves to the source-compatibility ctor emitted by the aspect in
        // project A, not to the mutated ctor. Treat that as a match for `target` when
        // the resolved ctor is a source-compatibility constructor in the same
        // declaring type whose parameters are a type+refkind prefix of `target`.
        if ( !resolved.IsSourceCompatibilityConstructor() )
        {
            return false;
        }

        if ( !SignatureTypeComparer.Instance.Equals( resolved.DeclaringType, target.DeclaringType ) )
        {
            return false;
        }

        if ( resolved.Parameters.Count >= target.Parameters.Count )
        {
            return false;
        }

        for ( var i = 0; i < resolved.Parameters.Count; i++ )
        {
            if ( !SignatureTypeComparer.Instance.Equals( resolved.Parameters[i].Type, target.Parameters[i].Type )
                 || resolved.Parameters[i].RefKind != target.Parameters[i].RefKind )
            {
                return false;
            }
        }

        return true;
    }

    public void PullConstructorParameterRecursive( IParameter baseParameter )
    {
        var baseConstructor = (IConstructor) baseParameter.DeclaringMember.AssertNotNull();
        var syntaxGenerationOptions = this.ServiceProvider.GetRequiredService<SyntaxGenerationOptions>();

        if ( !this._onlyProcessDerivedTypes )
        {
            // Process the current type.
            ProcessType(
                baseConstructor.DeclaringType.Constructors
                    .Where( c => c.InitializerKind == ConstructorInitializerKind.This ) );

            // Register a transitive aspect for the current type.
            if ( this._pullStrategy is not null && this._pullStrategy is not LegacyPullStrategy && baseConstructor.CanBeChainedFromOutsideAssembly() )
            {
                var transitiveAspect = new PullConstructorParameterTransitiveAspect(
                    this._pullStrategy,
                    baseParameter.ToRef(),
                    this._context.AspectOrder,
                    this._forwardingHelper?.OverloadingStrategy );

                this._context.AddTransitiveAspect(
                    new TransitiveAspectInstance(
                        transitiveAspect,
                        baseParameter.DeclaringMember.DeclaringType.ToRef(),
                        baseParameter.DeclaringMember.DeclaringType.Depth,
                        (IAspectClassImpl) this._context.AspectClassResolver.GetAspectClass( typeof(PullConstructorParameterTransitiveAspect) ),
                        this._aspectLayerInstance.AspectInstance.AspectState,
                        this._aspectLayerInstance.AspectInstance.PredecessorDegree + 1,
                        baseParameter.GetPrimarySyntaxTree() ) );
            }
        }

        // Process derived types.
        foreach ( var derivedType in this.Compilation.GetDerivedTypes( baseConstructor.DeclaringType, DerivedTypesOptions.DirectOnly ) )
        {
            ProcessType( derivedType.Constructors.Where( c => c.InitializerKind != ConstructorInitializerKind.This && !c.IsRecordCopyConstructor() ) );
        }

        return;

        void ProcessType( IEnumerable<IConstructor> potentialConstructors )
        {
            var chainedConstructors =
                potentialConstructors.Where( c => IsChainedCall( c, baseConstructor ) );

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
                        // We do not add a new argument and rely on the optional value.
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
                        parameterValue = SyntaxFactoryEx.SafeIdentifierName( pullParameterAction.ParameterName.AssertNotNull() );

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

                        if ( initializedChainedConstructor.CanBeChainedFromOutsideAssembly() )
                        {
                            recursiveParameterBuilder.AddAttribute( AttributeConstruction.Create( typeof(AspectGeneratedAttribute) ) );
                        }

                        recursiveParameterBuilder.Freeze();

                        this.AddTransformation( new IntroduceParameterTransformation( this._aspectLayerInstance, recursiveParameterBuilder.BuilderData ) );

                        var recursiveParameter = recursiveParameterBuilder;

                        // Generate a forwarding constructor preserving the chained constructor's original signature if requested.
                        this._forwardingHelper?.ApplyForwarderIfNeeded( initializedChainedConstructor, recursiveParameter );

                        // Process all constructors calling this constructor.
                        this.PullConstructorParameterRecursive( recursiveParameter );

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