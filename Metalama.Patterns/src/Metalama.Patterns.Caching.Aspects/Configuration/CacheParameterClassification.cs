// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

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

    public static CacheParameterClassification Default { get; } = new();

    public static CacheParameterClassification ExcludeFromCacheKey { get; } = new();

    public static CacheParameterClassification Ineligible( IDiagnostic diagnostic )
    {
        if ( diagnostic.Definition.Severity != Severity.Error )
        {
            throw new ArgumentOutOfRangeException( nameof(diagnostic), "The diagnostic must be an error." );
        }

        return new CacheParameterClassification( ( _, _ ) => diagnostic );
    }

    // ReSharper disable once RedundantSuppressNullableWarningExpression
    public static CacheParameterClassification Ineligible()
        => new(
            ( parameter, classifier )
                => CachingDiagnosticDescriptors.Cache.ParameterClassifiedAsIneligible.WithArguments(
                    ((IMethod) parameter.DeclaringMember, classifier.ToString()!, parameter.Name) ) );
}