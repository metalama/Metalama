// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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