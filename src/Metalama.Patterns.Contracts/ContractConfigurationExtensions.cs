// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Fabric extension methods to configure the <c>Metalama.Patterns.Contracts</c> namespace.
/// </summary>
[CompileTime]
public static class ContractConfigurationExtensions
{
    /// <summary>
    /// Configures <see cref="ContractOptions"/> for the current <see cref="ICompilation"/>.
    /// </summary>
    public static void ConfigureContracts( this IQuery<ICompilation> query, ContractOptions options ) => query.SetOptions( options );

    /// <summary>
    /// Configures <see cref="ContractOptions"/> for the given <see cref="INamespace"/>.
    /// </summary>
    public static void ConfigureContracts( this IQuery<INamespace> query, ContractOptions options ) => query.SetOptions( options );

    /// <summary>
    /// Configures <see cref="ContractOptions"/> for the given <see cref="INamedType"/>.
    /// </summary>
    public static void ConfigureContracts( this IQuery<INamedType> query, ContractOptions options ) => query.SetOptions( options );

    /// <summary>
    /// Configures <see cref="ContractOptions"/> for the given <see cref="IFieldOrPropertyOrIndexer"/>.
    /// </summary>
    public static void ConfigureContracts( this IQuery<IFieldOrPropertyOrIndexer> query, ContractOptions options )
        => query.SetOptions( options );

    /// <summary>
    /// Configures <see cref="ContractOptions"/> for the given <see cref="IMethod"/>.
    /// </summary>
    public static void ConfigureContracts( this IQuery<IMethod> query, ContractOptions options ) => query.SetOptions( options );

    /// <summary>
    /// Configures <see cref="ContractOptions"/> for the given <see cref="IParameter"/>.
    /// </summary>
    public static void ConfigureContracts( this IQuery<IParameter> query, ContractOptions options ) => query.SetOptions( options );
}