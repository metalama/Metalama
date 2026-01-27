// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.UserCode;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Advising;

internal sealed class AdviceFactoryState : IAdviceExecutionContext
{
    private readonly int _orderWithinType;
    private readonly ProjectServiceProvider _serviceProvider;
    private int _nextTransformationOrder;

    public CompilationModel MutableCompilation { get; }

    public IAspectInstanceInternal AspectInstance => this.AspectLayerInstance.AspectInstance;

    public AspectLayerInstance AspectLayerInstance { get; }

    public ref readonly ProjectServiceProvider ServiceProvider => ref this._serviceProvider;

    public CompilationModel InitialCompilation => this.AspectLayerInstance.InitialCompilation;

    public IDiagnosticAdder Diagnostics { get; }

    public IntrospectionPipelineListener? IntrospectionListener { get; }

    public ImmutableArray<ITransformation> Transformations { get; private set; } = ImmutableArray<ITransformation>.Empty;

    public AspectBuilderState? AspectBuilderState { get; set; }

    public UserCodeExecutionContext ExecutionContext { get; }

    public AdviceFactoryState(
        in ProjectServiceProvider serviceProvider,
        AspectLayerInstance aspectLayerInstance,
        CompilationModel currentCompilation,
        IDiagnosticAdder diagnostics,
        UserCodeExecutionContext executionContext,
        int pipelineStepIndex,
        int orderWithinType,
        IAspectClassResolver aspectClassResolver )
    {
        this.AspectOrder = pipelineStepIndex;
        this._orderWithinType = orderWithinType;
        this.AspectLayerInstance = aspectLayerInstance;
        this.MutableCompilation = currentCompilation;
        this._serviceProvider = serviceProvider;
        this.Diagnostics = diagnostics;
        this.IntrospectionListener = serviceProvider.GetService<IntrospectionPipelineListener>();
        this.ExecutionContext = executionContext;
        this.AspectClassResolver = aspectClassResolver;
    }

    public void AddTransformations( ImmutableArray<ITransformation> transformations )
    {
        this.Transformations = this.Transformations.AddRange( transformations );

        foreach ( var transformation in transformations )
        {
            if ( transformation.Observability != TransformationObservability.None )
            {
                this.MutableCompilation.AddTransformation( transformation );

                // Add implicit declarations (e.g., implicit static methods for extension block members) to the code model.
                foreach ( var implicitDeclaration in transformation.GetImplicitDeclarations() )
                {
                    this.MutableCompilation.AddDeclaration( implicitDeclaration );
                }

                if ( transformation is ISyntaxTreeTransformation syntaxTreeTransformation )
                {
                    UserCodeExecutionContext.CurrentOrNull?.AddDependencyTo( syntaxTreeTransformation.TransformedSyntaxTree );
                }
            }
        }
    }

    public void AddTransitiveAspects( ImmutableArray<TransitiveAspectInstance> aspects )
    {
        foreach ( var aspect in aspects )
        {
            this.AspectBuilderState.AssertNotNull().AddContributor( aspect );
        }
    }

    public void SetOrders( ITransformation transformation )
    {
        transformation.OrderWithinPipelineStepAndTypeAndAspectInstance = this._nextTransformationOrder++;
        transformation.OrderWithinPipelineStepAndType = this._orderWithinType;
        transformation.OrderWithinPipeline = this.AspectOrder;
    }

    public int AspectOrder { get; }

    public IAspectClassResolver AspectClassResolver { get; }
}