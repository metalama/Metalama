// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.DesignTime.Contracts.AspectExplorer;

[ComImport]
[Guid( "C0BDC548-BC2D-40C6-B9A8-96FEB4CCEEBA" )]
public interface IAspectDatabaseService : ICompilerService
{
    Task<IEnumerable<INamedTypeSymbol>> GetAspectClassesAsync( Compilation compilation, CancellationToken cancellationToken );

    [Obsolete]
    Task GetAspectInstancesAsync(
        Compilation compilation,
        INamedTypeSymbol aspectClass,
        IEnumerable<AspectExplorerAspectInstance>[] result,
        CancellationToken cancellationToken );

    event Action<string> AspectClassesChanged;

    event Action<string> AspectInstancesChanged;
}

[ComImport]
[Guid( "99E80D57-0C81-4461-B956-ECB1A7C3AA18" )]
public interface IAspectDatabaseService2 : IAspectDatabaseService
{
    Task GetAspectInstancesAsync(
        Compilation compilation,
        INamedTypeSymbol aspectClass,
        IEnumerable<IAspectExplorerAspectInstance>[] result,
        CancellationToken cancellationToken );
}