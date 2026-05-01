// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Diagnostics;

[ComImport]
[Guid( "AA73EC87-55AD-4135-8728-AEC30F3E9BB4" )]
public interface ICompileTimeErrorStatusService : ICompilerService
{
    IDiagnosticData[] CompileTimeErrors { get; }

    event Action? CompileTimeErrorsChanged;
}