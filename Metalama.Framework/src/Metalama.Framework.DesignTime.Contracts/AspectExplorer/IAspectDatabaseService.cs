// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.DesignTime.Contracts.AspectExplorer;

[ComImport]
[Guid( "F9D7E67E-3AA9-4783-9682-5EA3672DC399" )]
public interface IAspectDatabaseService : ICompilerService
{
    Task<IEnumerable<INamedTypeSymbol>> GetAspectClassesAsync( Compilation compilation, CancellationToken cancellationToken );

    [Obsolete]
    Task GetAspectInstancesAsync(
        Compilation compilation,
        INamedTypeSymbol aspectClass,
        IEnumerable<AspectExplorerAspectInstance>?[] result,
        CancellationToken cancellationToken );

    event Action<string> AspectClassesChanged;

    event Action<string> AspectInstancesChanged;
}

[ComImport]
[Guid( "AD1C0745-4D95-4338-8565-090A69D0A306" )]
public interface IAspectDatabaseService2 : IAspectDatabaseService
{
    Task GetAspectInstancesAsync(
        Compilation compilation,
        INamedTypeSymbol aspectClass,
        IEnumerable<IAspectExplorerAspectInstance>?[] result,
        CancellationToken cancellationToken );
}