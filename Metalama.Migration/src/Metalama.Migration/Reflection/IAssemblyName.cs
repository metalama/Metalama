// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using PostSharp.Constraints;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <see cref="IAssemblyIdentity"/>.
    /// </summary>
    [InternalImplement]
    public interface IAssemblyName
    {
        string Name { get; }

        Version Version { get; }

        byte[] GetPublicKey();

        byte[] GetPublicKeyToken();

        string Culture { get; }

        bool IsStronglyNamed { get; }

        bool IsRetargetable { get; }

        AssemblyProcessorArchitecture ProcessorArchitecture { get; }

        BindingContext BindingContext { get; }
    }

    /// <summary>
    /// The processor architecture is not exposed in Metalama.
    /// </summary>
    public enum AssemblyProcessorArchitecture
    {
        None = 0x0000,

        // ReSharper disable once InconsistentNaming
        MSIL = 0x0001,

        X86 = 0x0002,

        // ReSharper disable once InconsistentNaming
        IA64 = 0x0003,

        Amd64 = 0x0004,

        Arm = 0x0005,

        NoPlatform = 0x0007
    }
}