// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Utilities.Threading;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Fabrics;

/// <summary>
/// The top-level <see cref="IAspectSource"/> that implements fabrics.
/// </summary>
internal sealed class FabricAspectSource : IAspectSource
{
    private readonly FabricManager _fabricManager;

    private readonly ImmutableArray<TypeFabricDriver> _drivers; // Note that this list is ordered.
    private readonly ImmutableArray<IAspectClass> _aspectClasses;
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;

    public FabricAspectSource( FabricManager fabricManager, ImmutableArray<TypeFabricDriver> drivers )
    {
        this._concurrentTaskRunner = fabricManager.ServiceProvider.GetRequiredService<IConcurrentTaskRunner>();
        this._fabricManager = fabricManager;
        this._drivers = drivers;
        this._aspectClasses = ImmutableArray.Create<IAspectClass>( fabricManager.AspectClasses[FabricTopLevelAspectClass.FabricAspectName] );
    }

    ImmutableArray<IAspectClass> IAspectSource.AspectClasses => this._aspectClasses;

    public Task CollectAspectInstancesAsync( AspectInstanceCollector collector )
    {
        // Group drivers by their target declaration.
        var driversByTarget =
            this._drivers.Select( x => (Driver: x, Target: x.GetTargetIfInPartialCompilation( collector.Compilation )) )
                .Where( x => x.Target != null )
                .GroupBy( x => x.Target!, x => x.Driver );

        return this._concurrentTaskRunner.RunConcurrentlyAsync( driversByTarget, ProcessDriver, collector.CancellationToken );

        // Process target declarations.
        void ProcessDriver( IGrouping<IDeclaration, TypeFabricDriver> driverGroup )
        {
            var target = driverGroup.Key;

            // Create template classes for all fabrics.
            var drivers = driverGroup
                .Select(
                    driver => new FabricTemplateClass(
                        this._fabricManager.ServiceProvider,
                        driver,
                        driver.CompileTimeProject.TemplateReflectionContext ?? collector.Compilation.CompilationContext,
                        collector.Diagnostics,
                        null ) )
                .ToImmutableArray();

            // Create an aggregate aspect class composed of all fabric classes.
            var aggregateClass = new FabricAggregateAspectClass( drivers.As<TemplateClass>() );

            // Create a TemplateInstance for each fabric.
            var templateInstances = drivers.Select( d => new TemplateClassInstance( TemplateProvider.FromInstance( d.Driver.Fabric ), d ) )
                .ToImmutableArray();

            // Create an IAspect.
            IAspect aspect = target switch
            {
                INamedType => new FabricAspect<INamedType>( drivers ),
                INamespace => new FabricAspect<INamespace>( drivers ),
                ICompilation => new FabricAspect<ICompilation>( drivers ),
                _ => throw new AssertionFailedException( $"Unexpected fabric target: '{target}'." )
            };

            // Creates the aggregate AspectInstance for the target declaration.
            var aggregateInstance = new AspectInstance( aspect, target, aggregateClass, templateInstances, ImmutableArray<AspectPredecessor>.Empty );

            collector.AddAspectInstance( aggregateInstance );
        }
    }
}