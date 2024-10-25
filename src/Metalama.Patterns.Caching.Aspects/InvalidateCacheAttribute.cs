// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using JetBrains.Annotations;
using Metalama.Extensions.DependencyInjection;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Patterns.Caching.Aspects.Helpers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Metalama.Patterns.Caching.Aspects;

/// <summary>
/// Custom attribute that, when applied on a method, causes an invocation of this method to remove from
/// the cache the result of invocations of other given methods with the same parameter values. 
/// </summary>
[PublicAPI]
[AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
public sealed class InvalidateCacheAttribute : MethodAspect
{
    private readonly Type? _invalidatedMethodsDeclaringType;

    private readonly string[]? _invalidatedMethodNames;

    /// <summary>
    /// Gets or sets a value indicating whether the current <see cref="InvalidateCacheAttribute"/> can match several overloads of the methods.
    /// The default value is <c>false</c>, which means that an error will be emitted if the current <see cref="InvalidateCacheAttribute"/> matches
    /// several methods of the same name.
    /// </summary>
    public bool AllowMultipleOverloads { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidateCacheAttribute"/> class that invalidates method of the same type as the
    /// type to which the current <see cref="InvalidateCacheAttribute"/> aspect is being applied.
    /// </summary>
    /// <param name="methodNames">A list of names of methods to invalidate. All parameters of these methods (except those marked 
    /// with <see cref="NotCacheKeyAttribute"/>) must have a parameter of the same name and compatible type in the target
    /// method of the current <see cref="InvalidateCacheAttribute"/> aspect.</param>
    public InvalidateCacheAttribute( params string[] methodNames ) : this( null!, methodNames ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidateCacheAttribute"/> class  that invalidates method of a different type than the
    /// type to which the current <see cref="InvalidateCacheAttribute"/> aspect is being applied.
    /// </summary>
    /// <param name="declaringType">The type containing the methods to invalidate.</param>
    /// <param name="methodNames">A list of names of methods to invalidate. All parameters of these methods (except those marked 
    /// with <see cref="NotCacheKeyAttribute"/>) must have a parameter of the same name and compatible type in the target
    /// method of the current <see cref="InvalidateCacheAttribute"/> aspect.</param>
    public InvalidateCacheAttribute( Type declaringType, params string[] methodNames )
    {
        this._invalidatedMethodsDeclaringType = declaringType;
        this._invalidatedMethodNames = methodNames;
    }

    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            m => !m.ReturnType.GetAsyncInfo().IsAwaitable || m.ReturnType.IsTask(),
            _ => $"the return type must not be an awaitable type other than Task and Task<T>." );
    }

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // For each attribute:
        // - Validate attribute properties.
        // - Determine and validate

        var invalidatedMethods = new Dictionary<IMethod, InvalidatedMethodInfo>();

        var isValid = true;

        isValid &= ValidateAndFindInvalidatedMethods( builder, this, invalidatedMethods );

        foreach ( var secondaryInstance in builder.AspectInstance.SecondaryInstances )
        {
            isValid &= ValidateAndFindInvalidatedMethods( builder, (InvalidateCacheAttribute) secondaryInstance.Aspect, invalidatedMethods );
        }

        if ( !isValid )
        {
            return;
        }

        if ( invalidatedMethods.Count == 0 )
        {
            throw new CachingAssertionFailedException( "invalidatedMethods.Count == 0 not expected." );
        }

        // Create a field that stores the list of methods to be invalidated.
        var methodsInvalidatedByFieldName = builder.Target.ToSerializableId()
            .MakeAssociatedIdentifier( $"_methodsInvalidatedBy_{builder.Target.Name}" );

        var methodsInvalidatedByField = builder.Advice.IntroduceField(
            builder.Target.DeclaringType,
            methodsInvalidatedByFieldName,
            typeof(MethodInfo[]),
            IntroductionScope.Static,
            OverrideStrategy.Fail,
            b => b.Name = methodsInvalidatedByFieldName );

        builder.Advice.AddInitializer(
            builder.Target.DeclaringType,
            nameof(InitializeMethodInfoArray),
            InitializerKind.BeforeTypeConstructor,
            args: new { methods = invalidatedMethods.Keys.ToList(), field = methodsInvalidatedByField.Declaration } );

        // If any invalidated method uses dependency injection, also use dependency injection.
        IFieldOrProperty? cachingServiceField;

        if ( invalidatedMethods.Values.Any( m => m.UsesDependencyInjection ) )
        {
            if ( builder.Target.IsStatic )
            {
                builder.Diagnostics.Report( CachingDiagnosticDescriptors.MethodCannotBeStaticBecauseItUsesDependencyInjection.WithArguments( builder.Target ) );
                builder.SkipAspect();

                return;
            }

            if ( !builder.TryIntroduceDependency(
                    new DependencyProperties(
                        builder.Target.DeclaringType,
                        typeof(ICachingService),
                        "_cachingService" ),
                    out cachingServiceField ) )
            {
                builder.SkipAspect();

                return;
            }
        }
        else
        {
            cachingServiceField = null;
        }

        // Override the method.
        var asyncInfo = builder.Target.GetAsyncInfo();

        var templates = new MethodTemplateSelector(
            nameof(OverrideMethod),
            builder.Target.ReturnType.IsTask( withResult: false ) ? nameof(OverrideMethodAsyncTask) : nameof(OverrideMethodAsyncTaskOfT),
            useAsyncTemplateForAnyAwaitable: true );

        builder.Advice.Override(
            builder.Target,
            templates,
            args: new
            {
                returnType = asyncInfo.IsAwaitable ? asyncInfo.ResultType : builder.Target.ReturnType,
                methodsInvalidatedByField = methodsInvalidatedByField.Declaration,
                invalidatedMethods = invalidatedMethods.Values.OrderBy( x => x.Method.ToString() ),
                cachingServiceField
            } );
    }

    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedParameter.Global

    [Template]
    private static readonly FlashtraceSource _flashtraceSource = FlashtraceSource.Get( ((IType) meta.Tags["type"]!).ToTypeOfExpression().Value );

    [Template]
    public static void InitializeMethodInfoArray( IReadOnlyList<IMethod> methods, IField field )
    {
        var b = new ArrayBuilder( typeof(MethodInfo) );

        foreach ( var method in methods.OrderBy( x => x.ToString() ) )
        {
            // ReSharper disable once InvokeAsExtensionMethod
            b.Add( RunTimeHelpers.ThrowIfMissing( method.ToMethodInfo(), method.ToDisplayString() ) );
        }

        field.Value = b.ToValue();
    }

    [Template]
    private static dynamic? OverrideMethod(
        IEnumerable<InvalidatedMethodInfo> invalidatedMethods,
        IField methodsInvalidatedByField,
        IType returnType /* not used */,
        IField? cachingServiceField )
    {
        var cachingServiceExpression = cachingServiceField ?? ExpressionFactory.Capture( CachingService.Default );

        var result = meta.Proceed();

        var index = meta.CompileTime( 0 );

        foreach ( var invalidatedMethod in invalidatedMethods )
        {
            CachingServiceExtensions.Invalidate(
                (ICachingService) cachingServiceExpression.Value!,
                methodsInvalidatedByField.Value![index],
                invalidatedMethod.Method.IsStatic ? null : meta.This,
                MapArguments( invalidatedMethod ).Value );

            ++index;
        }

        return result;
    }

    private static IParameter? GetCancellationTokenParameter()
    {
        return meta.Target.Method.Parameters.OfParameterType( typeof(CancellationToken) ).LastOrDefault();
    }

    [Template]
    private static async Task<dynamic?> OverrideMethodAsyncTaskOfT(
        IEnumerable<InvalidatedMethodInfo> invalidatedMethods,
        IField methodsInvalidatedByField,
        IType returnType,
        IField? cachingServiceField )
    {
        var cancellationTokenExpression = GetCancellationTokenParameter() ?? ExpressionFactory.Capture( default(CancellationToken) );

        var cachingServiceExpression = cachingServiceField ?? ExpressionFactory.Capture( CachingService.Default );

        // ReSharper disable once RedundantAssignment
        var result = await meta.ProceedAsync();
        var index = meta.CompileTime( 0 );

        foreach ( var invalidatedMethod in invalidatedMethods )
        {
            await CachingServiceExtensions.InvalidateAsync(
                (ICachingService) cachingServiceExpression.Value!,
                methodsInvalidatedByField.Value![index],
                invalidatedMethod.Method.IsStatic ? null : meta.This,
                MapArguments( invalidatedMethod ).Value,
                cancellationTokenExpression.Value );

            ++index;
        }

        return result;
    }

    [Template]
    private static async Task OverrideMethodAsyncTask(
        IEnumerable<InvalidatedMethodInfo> invalidatedMethods,
        IField methodsInvalidatedByField,
        IType returnType /* not used */,
        IField? cachingServiceField )
    {
        var cancellationTokenExpression = GetCancellationTokenParameter() ?? ExpressionFactory.Capture( default(CancellationToken) );

        var cachingServiceExpression = cachingServiceField ?? ExpressionFactory.Capture( CachingService.Default );

        await meta.ProceedAsync();

        var index = meta.CompileTime( 0 );

        foreach ( var invalidatedMethod in invalidatedMethods )
        {
            await CachingServiceExtensions.InvalidateAsync(
                (ICachingService) cachingServiceExpression.Value!,
                methodsInvalidatedByField.Value![index],
                invalidatedMethod.Method.IsStatic ? null : meta.This,
                MapArguments( invalidatedMethod ).Value,
                cancellationTokenExpression.Value );

            ++index;
        }
    }

    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedParameter.Global

    private static IExpression MapArguments( InvalidatedMethodInfo invalidatedMethod )
    {
        var arrayBuilder = new ArrayBuilder();

        foreach ( var mappedArgumentPosition in invalidatedMethod.ParameterMap )
        {
            arrayBuilder.Add(
                mappedArgumentPosition >= 0
                    ? invalidatedMethod.Method.Parameters[mappedArgumentPosition]
                    : ExpressionFactory.Literal( 0 ) );
        }

        return arrayBuilder.ToExpression();
    }

    /// <summary>
    /// Validates the given aspect attribute. If valid, adds details of the invalidated methods to <paramref name="invalidatedMethods"/>.
    /// </summary>
    /// <returns><see langword="false"/> if any <see cref="Severity.Error"/> severity diagnostics are reported; otherwise, <see langword="false"/>.</returns>
    private static bool ValidateAndFindInvalidatedMethods(
        IAspectBuilder<IMethod> builder,
        InvalidateCacheAttribute attribute,
        Dictionary<IMethod, InvalidatedMethodInfo> invalidatedMethods )
    {
        if ( attribute._invalidatedMethodNames == null || attribute._invalidatedMethodNames.Length == 0 )
        {
            builder.Diagnostics.Report(
                CachingDiagnosticDescriptors.InvalidateCache.ErrorInvalidAspectConstructorNoMethodName.WithArguments( builder.Target ) );

            return false;
        }

        if ( attribute._invalidatedMethodNames.Any( string.IsNullOrWhiteSpace ) )
        {
            builder.Diagnostics.Report(
                CachingDiagnosticDescriptors.InvalidateCache.ErrorInvalidAspectConstructorNullOrWhitespaceString.WithArguments( builder.Target ) );

            return false;
        }

        var invalidatingMethod = builder.Target;

        var invalidatedMethodsDeclaringType = attribute._invalidatedMethodsDeclaringType == null
            ? invalidatingMethod.DeclaringType
            : (INamedType) TypeFactory.GetType( attribute._invalidatedMethodsDeclaringType );

        var invalidatingMethodParameters = invalidatingMethod.Parameters;
        var candidateInvalidatedMethods = invalidatedMethodsDeclaringType.AllMethods;

        DictionaryOfLists<string, IDiagnostic> matchingErrorsDictionary = new();
        DictionaryOfLists<string, InvalidatedMethodInfo?> invalidatedMethodsDictionary = new();

        var isValid = true;

        foreach ( var invalidatedMethod in candidateInvalidatedMethods )
        {
            if ( Array.IndexOf( attribute._invalidatedMethodNames, invalidatedMethod.Name ) == -1 )
            {
                continue;
            }

            if ( invalidatedMethods.ContainsKey( invalidatedMethod ) )
            {
                // Already processed: add null so that the later checks based on list count are handled correctly.
                invalidatedMethodsDictionary.Add( invalidatedMethod.Name, null );

                continue;
            }

            // Ensure the method is actually cached.
            var cacheAspectConfiguration =
                invalidatedMethod.Enhancements().GetAnnotations<CachedMethodAnnotation>().SingleOrDefault()?.Options;

            if ( cacheAspectConfiguration == null )
            {
                matchingErrorsDictionary.Add(
                    invalidatedMethod.Name,
                    CachingDiagnosticDescriptors.InvalidateCache.ErrorMethodIsNotCached.WithArguments( (builder.Target, invalidatedMethod) ) );

                continue;
            }

            // Check that the 'this' parameter is compatible.
            if ( !invalidatedMethod.IsStatic && !cacheAspectConfiguration.IgnoreThisParameter.GetValueOrDefault() &&
                 (invalidatingMethod.IsStatic || !(invalidatingMethod.DeclaringType == invalidatedMethod.DeclaringType
                                                   || invalidatingMethod.DeclaringType.DerivesFrom( invalidatedMethod.DeclaringType ))) )
            {
                matchingErrorsDictionary.Add(
                    invalidatedMethod.Name,
                    CachingDiagnosticDescriptors.InvalidateCache.ErrorThisParameterCannotBeMapped.WithArguments(
                        (invalidatingMethod, invalidatedMethod, invalidatingMethod.DeclaringType, invalidatedMethod.DeclaringType) ) );

                continue;
            }

            // Match parameters.
            var invalidatedMethodParameters = invalidatedMethod.Parameters;

            var invalidatedMethodInfo = new InvalidatedMethodInfo(
                invalidatedMethod,
                cacheAspectConfiguration.UseDependencyInjection.GetValueOrDefault( true ) );

            var allParametersMatching = true;

            for ( var i = 0; i < invalidatedMethodParameters.Count; i++ )
            {
                var invalidatedMethodParameter = invalidatedMethodParameters[i];

                if ( invalidatedMethodParameter.Attributes.Any( typeof(NotCacheKeyAttribute) ) )
                {
                    continue;
                }

                // Match parameter by name.
                var invalidatingMethodParameter =
                    invalidatingMethodParameters.OfName( invalidatedMethodParameter.Name );

                if ( invalidatingMethodParameter == null )
                {
                    matchingErrorsDictionary.Add(
                        invalidatedMethod.Name,
                        CachingDiagnosticDescriptors.InvalidateCache.ErrorMissingParameterInInvalidatingMethod.WithArguments(
                            (invalidatedMethod, invalidatedMethod, invalidatedMethodParameter.Name) ) );

                    allParametersMatching = false;

                    continue;
                }

                // Check that the type is compatible.
                if ( !invalidatingMethodParameter.Type.IsConvertibleTo( invalidatedMethodParameter.Type ) )
                {
                    matchingErrorsDictionary.Add(
                        invalidatedMethod.Name,
                        CachingDiagnosticDescriptors.InvalidateCache.ErrorParameterTypeIsNotCompatible.WithArguments(
                            (invalidatingMethod, invalidatedMethod, invalidatedMethodParameter.Name) ) );

                    allParametersMatching = false;

                    continue;
                }

                invalidatedMethodInfo.ParameterMap[i] = invalidatedMethodParameter.Index;
            }

            if ( !allParametersMatching )
            {
                continue;
            }

            invalidatedMethodsDictionary.Add( invalidatedMethod.Name, invalidatedMethodInfo );
        }

        foreach ( var invalidatedMethodName in attribute._invalidatedMethodNames )
        {
            if ( !invalidatedMethodsDictionary.TryGetList( invalidatedMethodName, out var invalidatedOverloads ) || invalidatedOverloads.Count == 0 )
            {
                if ( matchingErrorsDictionary.TryGetList( invalidatedMethodName, out var diagnostics ) )
                {
                    // There were diagnostics, but the method of the given name exists
                    foreach ( var diagnostic in diagnostics )
                    {
                        builder.Diagnostics.Report( diagnostic );
                    }
                }
                else
                {
                    // The method of the given name does not exist
                    builder.Diagnostics.Report(
                        CachingDiagnosticDescriptors.InvalidateCache.ErrorCachedMethodNotFound.WithArguments(
                            (invalidatingMethod, invalidatedMethodName, invalidatedMethodsDeclaringType) ) );
                }

                isValid = false;

                continue;
            }

            if ( !attribute.AllowMultipleOverloads && invalidatedOverloads.Count > 1 )
            {
                builder.Diagnostics.Report(
                    CachingDiagnosticDescriptors.InvalidateCache.ErrorMultipleOverloadsFound.WithArguments( (invalidatingMethod, invalidatedMethodName) ) );

                isValid = false;

                continue;
            }

            foreach ( var invalidatedMethodInfo in invalidatedOverloads )
            {
                // null indicates already processed (eg, via another aspect instance on the current target method)
                if ( invalidatedMethodInfo != null )
                {
                    // TODO: Reinstate equivalent annotations when supported by the Metalama framework.
#if false
                    builder.Diagnostics.Report( InfoMethodIsInvalidatedBy.WithArguments( invalidatingMethod ), invalidatedMethodInfo.Method );
                    builder.Diagnostics.Report( InfoMethodInvalidates.WithArguments( invalidatedMethodInfo.Method ), invalidatingMethod );
#endif

                    invalidatedMethods.Add( invalidatedMethodInfo.Method, invalidatedMethodInfo );
                }
            }
        }

        return isValid;
    }

    [CompileTime]
    private sealed class DictionaryOfLists<TKey, TValue> : IEnumerable<KeyValuePair<TKey, List<TValue>>>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, List<TValue>> _collectionsDictionary = new();

        public void Add( TKey methodName, TValue value )
        {
            if ( !this._collectionsDictionary.TryGetValue( methodName, out var list ) )
            {
                list = new List<TValue>();
                this._collectionsDictionary.Add( methodName, list );
            }

            list.Add( value );
        }

        public bool TryGetList( TKey key, [NotNullWhen( true )] out List<TValue>? list ) => this._collectionsDictionary.TryGetValue( key, out list );

        public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator() => this._collectionsDictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    [CompileTime]
    private sealed class InvalidatedMethodInfo
    {
        public InvalidatedMethodInfo( IMethod method, bool usesDependencyInjection )
        {
            this.Method = method;
            this.UsesDependencyInjection = usesDependencyInjection;
            this.ParameterMap = new int[method.Parameters.Count];

            for ( var i = 0; i < this.ParameterMap.Length; i++ )
            {
                this.ParameterMap[i] = -1;
            }
        }

        public IMethod Method { get; }

        public int[] ParameterMap { get; }

        public bool UsesDependencyInjection { get; }
    }
}