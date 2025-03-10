// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint
{
    /// <summary>
    /// Base interface for a implemented by the compiler part of the software (not the UI part) that
    /// can be returned synchronously.
    /// </summary>
    [ComImport]
    [Guid( "D174F35D-ABA7-4CDC-8B47-44E979019B3E" )]
    public interface ICompilerService;
}