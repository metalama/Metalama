// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;

namespace Metalama.Patterns.Wpf.Implementation.DependencyPropertyNamingConvention;

[CompileTime]
internal sealed class DefaultDependencyPropertyNamingConvention : IDependencyPropertyNamingConvention
{
    public static string RegistrationKey { get; } = "default";

    public string Name => "default";

    public DependencyPropertyNamingConventionMatch Match( IProperty targetProperty )
    {
        var propertyName = targetProperty.Name;

        return DependencyPropertyNamingConventionMatcher.Match(
            this,
            targetProperty,
            propertyName,
            GetRegistrationFieldNameFromPropertyName( propertyName ),
            new StringNameMatchPredicate( GetPropertyChangedMethodNameFromPropertyName( propertyName ) ),
            new StringNameMatchPredicate( GetValidateMethodNameFromPropertyName( propertyName ) ) );
    }

    internal static string GetRegistrationFieldNameFromPropertyName( string propertyName ) => $"{propertyName}Property";

    internal static string GetPropertyChangedMethodNameFromPropertyName( string propertyName ) => $"On{propertyName}Changed";

    internal static string GetValidateMethodNameFromPropertyName( string propertyName ) => $"Validate{propertyName}";
}