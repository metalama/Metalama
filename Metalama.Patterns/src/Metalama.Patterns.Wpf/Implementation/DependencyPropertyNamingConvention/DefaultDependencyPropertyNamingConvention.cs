// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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