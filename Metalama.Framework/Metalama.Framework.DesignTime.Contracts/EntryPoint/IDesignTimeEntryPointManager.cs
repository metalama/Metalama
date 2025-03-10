// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.EntryPoint
{
    /// <summary>
    /// Contract of the entry point of the API between the Metalama VSX and the Metalama analyzer, which can
    /// be both of different versions. This contract is strongly versioned. The reference to this API is stored
    /// on the <see cref="AppDomain"/> using <see cref="AppDomain.GetData"/> and <see cref="AppDomain.SetData(string,object)"/>.
    /// </summary>
    [ComImport]
    [Guid( "A0C85506-DB96-4C14-86E8-5F199731534B" )]
    public interface IDesignTimeEntryPointManager
    {
        /// <summary>
        /// Sets the logging delegate.
        /// </summary>
        void SetLogger( LogAction? logger );

        /// <summary>
        /// Gets an interface that allows to retrieve compiler services.
        /// </summary>
        /// <param name="contractVersions">A dictionary mapping the fixed version of the assembly (e.g. <c>1.0</c>)
        ///     to the contract version within this fixed version.</param>
        /// <returns></returns>
        IDesignTimeEntryPointConsumer GetConsumer( ContractVersion[] contractVersions );

        /// <summary>
        /// Registers a <see cref="ICompilerServiceProvider"/>. This method is called by the analyzer assembly.
        /// </summary>
        void RegisterServiceProvider( ICompilerServiceProvider entryPoint );

        /// <summary>
        /// Gets the version of the implementation of this interface.
        /// </summary>
        Version Version { get; }
    }
}