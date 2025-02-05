// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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