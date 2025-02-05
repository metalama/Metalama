// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint
{
    /// <summary>
    /// Entry point exposed by the compiler-side components, to be consumed by the Visual Studio extension.
    /// An instance of Visual Studio can contain several versions of Metalama compiler components, and they
    /// will be represented as different instances of this interface.
    /// </summary>
    [ComImport]
    [Guid( "C5D68E3C-F7A7-428E-91FC-090AE7EBA023" )]
    public interface ICompilerServiceProvider
    {
        /// <summary>
        /// Gets the version of the compiler-side component.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets a dictionary mapping the assembly fixed version (in form <c>Major.Minor</c>, e.g. <c>1.0</c> and the version
        /// of the contract within that fixed version. This contract version may change between pre-releases but it must remain
        /// constant in stable releases.
        /// </summary>
        ContractVersion[] ContractVersions { get; }

        /// <summary>
        /// Gets a service.
        /// </summary>
        ICompilerService? GetService( Type serviceType );
    }
}