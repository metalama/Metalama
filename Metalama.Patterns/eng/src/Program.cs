// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2025_1;

var product = new Product( MetalamaDependencies.MetalamaPatterns )
{
    Solutions =
    [
        new DotNetSolution( "Metalama.Patterns.sln" )
        { 
            CanFormatCode = true,
            FormatExclusions = ["src\\tests\\*AspectTests\\**\\*"]
        },
    ],
    PublicArtifacts = Pattern.Create(
        "Metalama.Patterns.Caching.$(PackageVersion).nupkg",
        "Metalama.Patterns.Caching.Aspects.$(PackageVersion).nupkg",
        "Metalama.Patterns.Caching.Backend.$(PackageVersion).nupkg",
        "Metalama.Patterns.Caching.Backends.Azure.$(PackageVersion).nupkg",
        "Metalama.Patterns.Caching.Backends.Redis.$(PackageVersion).nupkg",
        "Metalama.Patterns.Contracts.$(PackageVersion).nupkg",
        "Metalama.Patterns.Memoization.$(PackageVersion).nupkg",
        "Metalama.Patterns.Immutability.$(PackageVersion).nupkg",
        "Metalama.Patterns.Observability.$(PackageVersion).nupkg",
        "Metalama.Patterns.Wpf.$(PackageVersion).nupkg",
        "Flashtrace.$(PackageVersion).nupkg",
        "Flashtrace.Formatters.$(PackageVersion).nupkg" ),
    Dependencies = [DevelopmentDependencies.PostSharpEngineering, MetalamaDependencies.MetalamaExtensions],
    MainVersionDependency = MetalamaDependencies.Metalama,
    Configurations = Product.DefaultConfigurations.WithValue(
        BuildConfiguration.Public,
        Product.DefaultConfigurations.Public with
        {
            // This is the first version of Patters.
            // This line should be removed in the next version.
            RequiresUpstreamCheck = false
        } )
};

return new EngineeringApp( product ).Run( args );