// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Diagnostics;

[ComImport]
[Guid( "B3195FB8-73FF-47B9-9519-A50E2464A7F5" )]
public interface ICompileTimeErrorStatusService : ICompilerService
{
    IDiagnosticData[] CompileTimeErrors { get; }

    event Action? CompileTimeErrorsChanged;
}