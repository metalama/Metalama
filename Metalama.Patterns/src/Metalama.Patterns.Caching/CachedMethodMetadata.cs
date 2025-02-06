// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Formatters;
using Metalama.Patterns.Caching.Implementation;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Encapsulates information about a method being cached. This cached is used by the implementation of <see cref="ICachingService"/>
/// and you can use it if you override the <see cref="CacheKeyBuilder"/> class.
/// </summary>
[PublicAPI]
public sealed partial class CachedMethodMetadata
{
    private static int _nextId;

    internal int Id { get; } = Interlocked.Increment( ref _nextId );

    /// <summary>
    /// Gets the <see cref="MethodInfo"/> of the method.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets an array of <see cref="Parameter"/>.
    /// </summary>
    private ImmutableArray<Parameter> Parameters { get; }

    /// <summary>
    /// Gets a value indicating whether the return type of the method can be <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="ReturnValueCanBeNull"/> is only concerned with whether an instance of the type can be represented
    /// by <see langword="null"/>. For example, primitives like <see cref="int"/> and other non-nullable structs cannot
    /// be represented by <see langword="null"/>.
    /// </remarks>
    internal bool ReturnValueCanBeNull { get; }

    /// <summary>
    /// Gets the configuration that applies to the method and supplied by configuration custom attributes.
    /// </summary>
    /// <remarks>
    /// This already takes account of the any configuration custom attribute applied to parent classes and the assembly.
    /// </remarks>
    internal CachedMethodConfiguration Configuration { get; }

    /// <summary>
    /// Gets the awaitable result type for awaitable (eg, async) methods, or <see langword="null"/> where not applicable.
    /// </summary>
    /// <remarks>
    /// For example, for a method returning <see cref="Task{TResult}"/>, this would be the generic argument corresponding to <c>TResult</c>.
    /// </remarks>
    internal Type? AwaitableResultType { get; }

    public bool IgnoreThisParameter => this.Configuration.IgnoreThisParameter.GetValueOrDefault( false );

    public bool IsParameterIgnored( int index ) => this.Parameters[index].IsParameterIgnored;

    private CachedMethodMetadata(
        MethodInfo method,
        ImmutableArray<Parameter> parameters,
        CachedMethodConfiguration? buildTimeConfiguration )
    {
        this.Method = method;
        this.Parameters = parameters;
        this.Configuration = buildTimeConfiguration ?? CachedMethodConfiguration.Empty;

        this.ReturnValueCanBeNull = !method.ReturnType.IsValueType
                                    || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Nullable<>));

        this.AwaitableResultType = method.ReturnType;

        if ( method.ReturnType.IsGenericType )
        {
            var genericType = method.ReturnType.GetGenericTypeDefinition();

            if ( genericType == typeof(Task<>) || genericType == typeof(ValueTask<>) )
            {
                this.AwaitableResultType = method.ReturnType.GenericTypeArguments[0];
            }
        }
    }

    public static CachedMethodMetadata Register(
        MethodInfo method,
        CachedMethodConfiguration? buildTimeConfiguration = null,
        bool throwIfAlreadyRegistered = true )
    {
        var metadata = new CachedMethodMetadata(
            method,
            GetCachedParameterInfos( method ),
            buildTimeConfiguration );

        return CachedMethodMetadataRegistry.Instance.Register( metadata, throwIfAlreadyRegistered );
    }

    [MethodImpl( MethodImplOptions.NoInlining )]
    public static CachedMethodMetadata ForCallingMethod( CachedMethodConfiguration? configuration = null, int skipFrames = 0 )
    {
        var stackFrame = new StackFrame( 1 + skipFrames );
        var methodInfo = (MethodInfo?) stackFrame.GetMethod() ?? throw new InvalidOperationException( "Cannot get the calling method." );

        var existingMetadata = CachedMethodMetadataRegistry.Instance.Get( methodInfo );

        if ( existingMetadata != null )
        {
            return existingMetadata;
        }

        return Register( methodInfo, configuration, false );
    }

    private static ImmutableArray<Parameter> GetCachedParameterInfos( MethodInfo method )
    {
        var parameterInfos = method.GetParameters();
        var cachedParameterInfos = new Parameter[parameterInfos.Length];

        for ( var i = 0; i < parameterInfos.Length; i++ )
        {
            var isIgnored = IsIgnored( parameterInfos[i] );

            cachedParameterInfos[i] = new Parameter( isIgnored );
        }

        return [..cachedParameterInfos];
    }

    private static bool IsIgnored( ParameterInfo parameter )
        => parameter.IsDefined( typeof(NotCacheKeyAttribute) ) || parameter.ParameterType == typeof(CancellationToken);
}