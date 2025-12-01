// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

/// <summary>
/// Represents the result of classifying a method parameter for cache key generation.
/// </summary>
/// <remarks>
/// <para>This class is returned by <see cref="ICacheParameterClassifier.GetClassification"/> and determines
/// how a parameter is handled by the caching aspect:</para>
/// <list type="bullet">
/// <item><description><see cref="Default"/>: The parameter is included in the cache key using standard formatting.</description></item>
/// <item><description><see cref="ExcludeFromCacheKey"/>: The parameter is excluded from the cache key.</description></item>
/// <item><description><see cref="Ineligible()"/>: The method cannot be cached because of this parameter (reports a compile-time error).</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ICacheParameterClassifier"/>
/// <seealso cref="CachingOptionsBuilder.AddParameterClassifier"/>
[PublicAPI]
[CompileTime]
public sealed class CacheParameterClassification
{
    private readonly Func<IParameter, ICacheParameterClassifier, IDiagnostic>? _diagnostic;

    private CacheParameterClassification( Func<IParameter, ICacheParameterClassifier, IDiagnostic>? diagnostic = null )
    {
        this._diagnostic = diagnostic;
    }

    internal bool IsIneligible => this._diagnostic != null;

    internal IDiagnostic GetDiagnostic( IParameter parameter, ICacheParameterClassifier classifier ) => this._diagnostic!.Invoke( parameter, classifier );

    /// <summary>
    /// Gets a classification indicating that the parameter should be included in the cache key using standard formatting.
    /// </summary>
    public static CacheParameterClassification Default { get; } = new();

    /// <summary>
    /// Gets a classification indicating that the parameter should be excluded from the cache key.
    /// The <see cref="NotCacheKeyAttribute"/> will be automatically added to the parameter.
    /// </summary>
    public static CacheParameterClassification ExcludeFromCacheKey { get; } = new();

    /// <summary>
    /// Creates a classification indicating that the method cannot be cached because of this parameter,
    /// with a custom error diagnostic.
    /// </summary>
    /// <param name="diagnostic">The error diagnostic to report. Must have <see cref="Severity.Error"/> severity.</param>
    /// <returns>A classification that will cause a compile-time error with the specified diagnostic.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the diagnostic does not have <see cref="Severity.Error"/> severity.</exception>
    public static CacheParameterClassification Ineligible( IDiagnostic diagnostic )
    {
        if ( diagnostic.Definition.Severity != Severity.Error )
        {
            throw new ArgumentOutOfRangeException( nameof(diagnostic), "The diagnostic must be an error." );
        }

        return new CacheParameterClassification( ( _, _ ) => diagnostic );
    }

    /// <summary>
    /// Creates a classification indicating that the method cannot be cached because of this parameter,
    /// with a default error diagnostic.
    /// </summary>
    /// <returns>A classification that will cause a compile-time error with a standard diagnostic message.</returns>
    // ReSharper disable once RedundantSuppressNullableWarningExpression
    public static CacheParameterClassification Ineligible()
        => new(
            ( parameter, classifier )
                => CachingDiagnosticDescriptors.Cache.ParameterClassifiedAsIneligible.WithArguments(
                    ((IMethod) parameter.DeclaringMember!, classifier.ToString()!, parameter.Name) ) );
}