// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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