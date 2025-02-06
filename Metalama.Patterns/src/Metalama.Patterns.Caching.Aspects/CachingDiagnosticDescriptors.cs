// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using static Metalama.Framework.Diagnostics.Severity;

// ReSharper disable InconsistentNaming

namespace Metalama.Patterns.Caching.Aspects;

[CompileTime]
internal static class CachingDiagnosticDescriptors
{
    private const string _category = "Metalama.Patterns.Caching";

    // Reserved range 5100-5120

    // Original PS CAC001 and CAC010 apply to [Cache] and are handled by eligibility.

    public static DiagnosticDefinition<IMethod> MethodCannotBeStaticBecauseItUsesDependencyInjection { get; } = new(
        "LAMA5110",
        Error,
        "The method '{0}' cannot be cached because it is static and caching is configured to use dependency injection. " +
        "Disable dependency injection using CacheAttribute.UseDependencyInjection or CacheConfigurationAttribute.UseDependencyInjection or make the method non-static.",
        "The method cannot be cached because it is static and caching is configured to use dependency injection." );

    public static class InvalidateCache
    {
        /// <summary>
        /// Was PS CAC002:
        /// The [InvalidateCache] aspect applied to {0} cannot invalidate {1}: this method is not cached by the [Cache] aspect.
        /// </summary>
        public static readonly DiagnosticDefinition<(IMethod InvalidatingMethod, IMethod InvalidatedMethod)> ErrorMethodIsNotCached =
            new(
                "LAMA5100",
                Error,
                "The [InvalidateCache] aspect applied to '{0}' cannot invalidate '{1}': this method is not cached by the [Cache] aspect.",
                "Method is not cached.",
                _category );

        /// <summary>
        /// Was PS CAC003:
        /// The [InvalidateCache] aspect applied to {0} cannot invalidate {1}: the 'this' parameter cannot be mapped because (a) this is an instance method, (b) the method, class or assembly is not annotated with the [CacheConfiguration( IgnoreThisParameter = true )]] attribute and (c) the type {2} is not derived from {3}.
        /// </summary>
        public static readonly
            DiagnosticDefinition<(IMethod InvalidatingMethod, IMethod CachedMethod, IType InvalidatingMethodDeclaringType, IType CachedMethodDeclaringType)>
            ErrorThisParameterCannotBeMapped =
                new(
                    "LAMA5101",
                    Error,
                    "The [InvalidateCache] aspect applied to '{0}' cannot invalidate '{1}': the 'this' parameter cannot be mapped because " +
                    "(a) '{1}' is an instance method, (b) the IgnoreThisParameter is not enabled and (c) the type {2} is either static or not derived from '{3}'.",
                    "'this' parameter cannot be mapped.",
                    _category );

        /// <summary>
        /// Was PS CAC004:
        /// The [InvalidateCache] aspect applied to {0} cannot invalidate {1}: the cached method does not contain a parameter named '{2}'. Make sure {0} contains a parameter named '{2}' or add the [NotCachedKey] attribute to the '{2}' parameter in {1}.
        /// </summary>
        public static readonly DiagnosticDefinition<(IMethod InvalidatingMethod, IMethod CachedMethod, string ParameterName)>
            ErrorMissingParameterInInvalidatingMethod =
                new(
                    "LAMA5102",
                    Error,
                    "The [InvalidateCache] aspect applied to '{0}' cannot invalidate '{1}': the invalidating method does not contain a parameter named '{2}'. "
                    +
                    "Make sure '{0}' contains a parameter named '{2}' or add the [NotCachedKey] attribute to the '{2}' parameter in '{1}'.",
                    "No matching parameter in invalidating method.",
                    _category );

        /// <summary>
        /// Was PS CAC005:
        /// The [InvalidateCache] aspect applied to {0} cannot invalidate {1}: the type of the '{2}' parameter of the cached is not compatible with the type of the '{2}' parameter in the invalidating method.
        /// </summary>
        public static readonly DiagnosticDefinition<(IMethod InvalidatingMethod, IMethod CachedMethod, string ParameterName)>
            ErrorParameterTypeIsNotCompatible =
                new(
                    "LAMA5103",
                    Error,
                    "The [InvalidateCache] aspect applied to '{0}' cannot invalidate '{1}': the type of the '{2}' parameter of the cached method is not compatible "
                    +
                    "with the type of the '{2}' parameter in the invalidating method.",
                    "Parameter type is not compatible.",
                    _category );

        /// <summary>
        /// Was PS CAC006:
        /// Invalid [InvalidateCache] aspect on {0}: the constructor parameters cannot contain a null or empty string.
        /// </summary>
        public static readonly DiagnosticDefinition<IMethod> ErrorInvalidAspectConstructorNullOrWhitespaceString =
            new(
                "LAMA5104",
                Error,
                "Invalid [InvalidateCache] aspect on '{0}': the constructor parameters cannot contain a null, empty or whitespace-only string.",
                "[InvalidateCache] parameters cannot contain a null, empty or whitespace string.",
                _category );

        /// <summary>
        /// Was PS CAC007:
        /// Invalid [InvalidateCache] aspect on {0}: the constructor parameters must contain at least one method name.
        /// </summary>
        public static readonly DiagnosticDefinition<IMethod> ErrorInvalidAspectConstructorNoMethodName =
            new(
                "LAMA5105",
                Error,
                "Invalid [InvalidateCache] aspect on '{0}': the constructor parameters must contain at least one method name.",
                "[InvalidateCache] must contain at least one method name.",
                _category );

        /// <summary>
        /// Was PS CAC008:
        /// Invalid [InvalidateCache] aspect on {0}: there is no method named {1} in type {2}.
        /// </summary>
        public static readonly DiagnosticDefinition<(IMethod InvalidatingMethod, string MethodName, IType CachedMethodDeclaringType)>
            ErrorCachedMethodNotFound =
                new(
                    "LAMA5106",
                    Error,
                    "Invalid [InvalidateCache] aspect on '{0}': there is no method named '{1}' in type '{2}'.",
                    "Cached method not found.",
                    _category );

        /// <summary>
        /// Was PS CAC009:
        /// Invalid [InvalidateCache] aspect on {0}: there are several suitable overloads of the {1} method. Set the AllowMultipleOverloads property to \"true\" to allow invalidation of all of them.
        /// </summary>
        public static readonly DiagnosticDefinition<(IMethod InvalidatingMethod, string MethodName)> ErrorMultipleOverloadsFound =
            new(
                "LAMA5107",
                Error,
                "Invalid [InvalidateCache] aspect on '{0}': there are several suitable overloads of the '{1}' method. Set the AllowMultipleOverloads property to \"true\" to allow invalidation of all of them.",
                "Multiple suitable overloads found.",
                _category );

        // TODO: Reinstate equivalent annotations when supported by the Metalama framework.
#if false
        public static readonly DiagnosticDefinition<IMethod> InfoMethodIsInvalidatedBy =
            new(
                "LAMA5108",
                Info,
                "This method is invalidated by {0}.",
                "Invalidated by an [InvalidateCache] method.",
                _cagtegory );

        public static readonly DiagnosticDefinition<IMethod> InfoMethodInvalidates =
            new(
                "LAMA5109",
                Info,
                "This method invalidates {0}.",
                "Invalidates a [Cache] method.",
                _cagtegory );
#endif
    }

    public static class Cache
    {
        /// <summary>
        /// The [Cache] aspect can be applied at most once to a method.
        /// </summary>
        public static readonly DiagnosticDefinition ErrorMultipleInstancesOfCachedAttribute =
            new(
                "LAMA5110",
                Error,
                "The [Cache] aspect can be applied at most once to a method.",
                "[Cache] aspect applied more than once.",
                _category );

        public static readonly DiagnosticDefinition<(IMethod Method, string Classifier, string ParameterName)> ParameterClassifiedAsIneligible =
            new(
                "LAMA5111",
                Error,
                "The [Cache] aspect can be applied to the method '{0}' because the classifier '{1}' marked the  parameter '{2}' as ineligible.",
                "[Cache] aspect cannot be applied to the method because a parameter is ineligible.",
                _category );
    }
}