// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Observability.Configuration;

/// <summary>
/// Extension methods that configure the <see cref="ObservableAttribute"/> aspect.
/// </summary>
[PublicAPI]
[CompileTime]
public static class ObservabilityExtensions
{
    /// <summary>
    /// Configures <see cref="ObservableAttribute"/> for the current project.
    /// </summary>
    /// <param name="query">The <see cref="IQuery{TDeclaration}"/> for the current compilation.</param>
    /// <param name="configure">A delegate that configures the aspect.</param>
    public static void ConfigureObservability(
        this IQuery<ICompilation> query,
        Action<ObservabilityTypeOptionsBuilder> configure )
    {
        var builder = new ObservabilityTypeOptionsBuilder();
        configure( builder );

        if ( builder.ObservabilityOptions != null )
        {
            query.SetOptions( builder.ObservabilityOptions );
        }

        if ( builder.ClassicStrategyOptions != null )
        {
            query.SetOptions( builder.ClassicStrategyOptions );
        }

        if ( builder.DependencyAnalysisOptions != null )
        {
            query.SetOptions( builder.DependencyAnalysisOptions );
        }
    }

    /// <summary>
    /// Configures <see cref="ObservableAttribute"/> for the current namespace.
    /// </summary>
    /// <param name="query">The <see cref="IQuery{TDeclaration}"/> for the current namespace.</param>
    /// <param name="configure">A delegate that configures the aspect.</param>
    public static void ConfigureObservability(
        this IQuery<INamespace> query,
        Action<ObservabilityTypeOptionsBuilder> configure )
    {
        var builder = new ObservabilityTypeOptionsBuilder();
        configure( builder );

        if ( builder.ObservabilityOptions != null )
        {
            query.SetOptions( builder.ObservabilityOptions );
        }

        if ( builder.ClassicStrategyOptions != null )
        {
            query.SetOptions( builder.ClassicStrategyOptions );
        }

        if ( builder.DependencyAnalysisOptions != null )
        {
            query.SetOptions( builder.DependencyAnalysisOptions );
        }
    }

    /// <summary>
    /// Configures <see cref="ObservableAttribute"/> for the current type.
    /// </summary>
    /// <param name="query">The <see cref="IQuery{TDeclaration}"/> for the current type.</param>
    /// <param name="configure">A delegate that configures the aspect.</param>
    public static void ConfigureObservability(
        this IQuery<INamedType> query,
        Action<ObservabilityTypeOptionsBuilder> configure )
    {
        var builder = new ObservabilityTypeOptionsBuilder();
        configure( builder );

        if ( builder.ObservabilityOptions != null )
        {
            query.SetOptions( builder.ObservabilityOptions );
        }

        if ( builder.ClassicStrategyOptions != null )
        {
            query.SetOptions( builder.ClassicStrategyOptions );
        }

        if ( builder.DependencyAnalysisOptions != null )
        {
            query.SetOptions( builder.DependencyAnalysisOptions );
        }
    }

    /// <summary>
    /// Configures <see cref="ObservableAttribute"/> for the current member.
    /// </summary>
    /// <param name="query">The <see cref="IQuery{TDeclaration}"/> for the current type.</param>
    /// <param name="configure">A delegate that configures the aspect.</param>
    public static void ConfigureObservability(
        this IQuery<IMember> query,
        Action<ObservabilityMemberOptionsBuilder> configure )
    {
        var builder = new ObservabilityMemberOptionsBuilder();
        configure( builder );

        if ( builder.DependencyAnalysisOptions != null )
        {
            query.SetOptions( builder.DependencyAnalysisOptions );
        }
    }
}