// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2025_1;

var product = new Product( MetalamaDependencies.MetalamaExtensions )
{
    Solutions =
    [
        new DotNetSolution( "Metalama.Extensions.sln" ) 
        { 
            CanFormatCode = true,
            FormatExclusions = ["src\\tests\\*AspectTests\\**\\*"],
        }
        
    ],
    PublicArtifacts = Pattern.Create(
        "Metalama.Extensions.DependencyInjection.$(PackageVersion).nupkg",
        "Metalama.Extensions.DependencyInjection.ServiceLocator.$(PackageVersion).nupkg",
        "Metalama.Extensions.Multicast.$(PackageVersion).nupkg",
        "Metalama.Extensions.Metrics.$(PackageVersion).nupkg" ),
    Dependencies = [DevelopmentDependencies.PostSharpEngineering, MetalamaDependencies.Metalama],
    MainVersionDependency = MetalamaDependencies.Metalama
};

return new EngineeringApp( product ).Run( args );