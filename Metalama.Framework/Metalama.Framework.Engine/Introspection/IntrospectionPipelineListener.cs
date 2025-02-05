// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionPipelineListener : IProjectService
{
    private readonly IntrospectionFactory _introspectionFactory;

    public IntrospectionPipelineListener( in ProjectServiceProvider serviceProvider )
    {
        this._introspectionFactory = serviceProvider.GetRequiredService<IntrospectionFactory>();
    }

    public void AddAdviceResult( IAspectInstance aspectInstance, Advice advice, AdviceResult result, ICompilation compilation )
    {
        var introspectionAdviceInstance = this._introspectionFactory.GetIntrospectionAspectInstance( aspectInstance );
        introspectionAdviceInstance.Advice.Add( new IntrospectionAdvice( advice, result, compilation, this._introspectionFactory ) );
    }

    public void AddAspectResult( AspectInstanceResult result )
    {
        var introspectionAdviceInstance = this._introspectionFactory.GetIntrospectionAspectInstance( result.AspectInstance );
        introspectionAdviceInstance.AspectInstanceResult = result;

        foreach ( var predecessor in result.AspectInstance.Predecessors )
        {
            var predecessorObject = this._introspectionFactory.GetIntrospectionAspectPredecessor( predecessor.Instance );
            predecessorObject.AddSuccessor( predecessor );
        }
    }

#pragma warning disable CA1822 // Mark members as static

    // ReSharper disable UnusedParameter.Global
    public void AddStaticFabricResult( StaticFabricResult result ) { }

    public void AddStaticFabricFailure( StaticFabricDriver driver ) { }
#pragma warning restore CA1822 // Mark members as static
}