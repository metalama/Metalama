// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.DesignTime.Contracts.Pipeline;

[ComImport]
[Guid( "63D30200-2953-4967-BF65-8A693B26ED7E" )]
public interface ITransitiveCompilationService : ICompilerService
{
    ValueTask GetTransitiveAspectManifestAsync(
        Compilation compilation,
        ITransitiveCompilationResult?[] result,
        CancellationToken cancellationToken );
}