// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Reflection;

[assembly: AssemblyVersion( "1.1.0.0" )]

// Makes the whole assembly compile-time without Metalama transforming it, which is what the public assembly of an
// SDK-based or weaver-based aspect does.
[assembly: CompileTime]

namespace Contract;

/// <summary>
/// A compile-time helper of version 1.1 of the <c>Contract</c> assembly.
/// </summary>
public static class Helper
{
    /// <summary>
    /// Gets a message. No project names this member: it exists only so that the assembly has content.
    /// </summary>
    public static string Message => "Contract 1.1";
}
