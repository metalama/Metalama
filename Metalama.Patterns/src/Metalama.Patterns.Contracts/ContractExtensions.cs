// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Fabrics;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Fabric extension methods allowing to add <see cref="NotNullAttribute"/> contracts in bulk and to access options.
/// </summary>
[CompileTime]
[PublicAPI]
public static class ContractExtensions
{
    /// <summary>
    /// Add the <see cref="NotNullAttribute"/> aspect to all public, reference typed, non-nullable fields, properties and parameters in the compilation.
    /// The <paramref name="includeInternalApis"/> parameter allows to enlarge the set to internal and private APIs.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="includeInternalApis">Determines whether the non-public fields, properties and parameters should be included.</param>
    /// <seealso href="@enforcing-non-nullability"/>
    public static void VerifyNotNullableDeclarations( this IQuery<ICompilation> compilation, bool includeInternalApis = false )
    {
        bool IsVisible( INamedType t ) => includeInternalApis || t.Accessibility.IsSupersetOrEqual( Accessibility.Protected );

        // Select types.
        var types = compilation.SelectMany( t => t.AllTypes )
            .Where( IsVisible );

        types.VerifyNotNullableDeclarations( includeInternalApis );
    }

    /// <summary>
    /// Add the <see cref="NotNullAttribute"/> aspect to all public, reference typed, non-nullable fields, properties and parameters in the given namespaces.
    /// The <paramref name="includeInternalApis"/> parameter allows to enlarge the set to internal and private APIs.
    /// </summary>
    /// <param name="ns">A collection of namespaces.</param>
    /// <param name="includeInternalApis">Determines whether the non-public fields, properties and parameters should be included.</param>
    /// <seealso href="@enforcing-non-nullability"/>
    public static void VerifyNotNullableDeclarations( this IQuery<INamespace> ns, bool includeInternalApis = false )
    {
        bool IsVisible( INamedType t ) => includeInternalApis || t.Accessibility.IsSupersetOrEqual( Accessibility.Protected );

        // Select types.
        var types = ns.SelectMany( t => t.Types )
            .Where( IsVisible );

        types.VerifyNotNullableDeclarations( includeInternalApis );
    }

    /// <summary>
    /// Add the <see cref="NotNullAttribute"/> aspect to all public, reference typed, non-nullable fields, properties and parameters in the given types.
    /// The <paramref name="includeInternalApis"/> parameter allows to enlarge the set to internal and private APIs.
    /// </summary>
    /// <param name="types">A collection of types.</param>
    /// <param name="includeInternalApis">Determines whether the non-public fields, properties and parameters should be included.</param>
    /// <seealso href="@enforcing-non-nullability"/>
    public static void VerifyNotNullableDeclarations( this IQuery<INamedType> types, bool includeInternalApis = false )
    {
        bool IsVisible( IMember t ) => includeInternalApis || t.Accessibility.IsSupersetOrEqual( Accessibility.Protected );

        var requiredAttribute = (INamedType) TypeFactory.GetType( typeof(RequiredAttribute) );
        var notNullableAttribute = (INamedType) TypeFactory.GetType( typeof(NotNullAttribute) );

        static bool IsNullableType( IHasType d ) => d is { Type: { IsReferenceType: true, IsNullable: false }, RefKind: RefKind.None };

        IAttribute? GetNotNullAspectAttribute( IDeclaration d )
            => d.Attributes.OfAttributeType( requiredAttribute ).FirstOrDefault() ??
               d.Attributes.OfAttributeType( notNullableAttribute ).FirstOrDefault();

        // Add aspects to fields, properties and indexers.
        var fieldsAndProperties = types
            .SelectMany(
                t => t.Properties
                    .Union<IFieldOrPropertyOrIndexer>( t.Fields )
                    .Union( t.Indexers ) )
            .Where( f => IsVisible( f ) && IsNullableType( f ) && f.Attributes.All( a => a.Type.Name != "AllowNullAttribute" ) );

        fieldsAndProperties
            .Where( f => GetNotNullAspectAttribute( f ) == null && f.Writeability is Writeability.InitOnly or Writeability.All )
            .AddAspectIfEligible<NotNullAttribute>();

        // Add aspects to method, constructor and indexer parameters.
        var parameters = types
            .SelectMany( t => t.Methods.Concat<IHasParameters>( t.Constructors ).Concat( t.Indexers ) )
            .Where( IsVisible )
            .SelectMany( t => t.Parameters )
            .Where( IsNullableType );

        parameters
            .Where( parameter => GetNotNullAspectAttribute( parameter ) == null )
            .AddAspectIfEligible<NotNullAttribute>();

        // Warn if the attribute is duplicate.
        fieldsAndProperties.Where( f => GetNotNullAspectAttribute( f ) != null )
            .ReportDiagnostic(
                f =>
                {
                    var nullableAttribute = GetNotNullAspectAttribute( f );

                    return ContractDiagnostics.ContractRedundant.WithArguments( (f, nullableAttribute!.Type.Name) );
                } );

        parameters.Where( f => GetNotNullAspectAttribute( f ) != null )
            .ReportDiagnostic(
                f =>
                {
                    var nullableAttribute = GetNotNullAspectAttribute( f );

                    return ContractDiagnostics.ContractRedundant.WithArguments( (f, nullableAttribute!.Type.Name) );
                } );
    }

    /// <summary>
    /// Gets the <see cref="ContractOptions"/> in the context of the current template.
    /// </summary>
    /// <param name="target">The value of <c>meta.Target</c>.</param>
    /// <seealso href="@configuring-contracts"/>
    [Obsolete( "Use ContractContext.Options" )]
    public static ContractOptions GetContractOptions( this IMetaTarget target ) => target.Declaration.GetContractOptions();

    /// <summary>
    /// Gets the <see cref="ContractOptions"/> for a given method.
    /// </summary>
    /// <seealso href="@configuring-contracts"/>
    public static ContractOptions GetContractOptions( this IMethod declaration ) => declaration.Enhancements().GetOptions<ContractOptions>();

    /// <summary>
    /// Gets the <see cref="ContractOptions"/> for a given type.
    /// </summary>
    /// <seealso href="@configuring-contracts"/>
    public static ContractOptions GetContractOptions( this INamedType declaration ) => declaration.Enhancements().GetOptions<ContractOptions>();

    /// <summary>
    /// Gets the <see cref="ContractOptions"/> for a given field, property, or indexer.
    /// </summary>
    /// <seealso href="@configuring-contracts"/>
    public static ContractOptions GetContractOptions( this IFieldOrPropertyOrIndexer declaration ) => declaration.Enhancements().GetOptions<ContractOptions>();

    /// <summary>
    /// Gets the <see cref="ContractOptions"/> for a given parameter.
    /// </summary>
    /// <seealso href="@configuring-contracts"/>
    public static ContractOptions GetContractOptions( this IParameter declaration ) => declaration.Enhancements().GetOptions<ContractOptions>();

    /// <summary>
    /// Gets the <see cref="ContractOptions"/> for a given declaration.
    /// </summary>
    /// <seealso href="@configuring-contracts"/>
    public static ContractOptions GetContractOptions( this IDeclaration declaration )
        => declaration switch
        {
            IParameter parameter => parameter.Enhancements().GetOptions<ContractOptions>(),
            IFieldOrPropertyOrIndexer field => field.Enhancements().GetOptions<ContractOptions>(),
            INamedType namedType => namedType.Enhancements().GetOptions<ContractOptions>(),
            IMethod method => method.Enhancements().GetOptions<ContractOptions>(),
            _ => throw new ArgumentOutOfRangeException()
        };
}