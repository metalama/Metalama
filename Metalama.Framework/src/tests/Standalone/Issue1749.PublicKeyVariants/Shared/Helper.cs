// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Contract;

/// <summary>
/// A compile-time helper compiled into both <c>Contract1</c> and <c>Contract2</c>, from this single file.
/// </summary>
/// <remarks>
/// <para>
/// The single shared file is what makes the two compile-time projects collide. <c>ComputeSourceHash</c> hashes each
/// compile-time syntax tree's file path as well as its text (deliberately, for #730), so two textually identical
/// copies at two paths would hash differently and no collision would occur. Both projects therefore include this one
/// file through a path normalized with <c>[System.IO.Path]::GetFullPath</c>, so that the compiler receives the same
/// literal path from each. See the README.
/// </para>
/// <para>
/// The type must be neither an aspect, nor a fabric, nor a template provider, nor an option type. A duplicate of any
/// of those is reported before the closure dictionary is ever built, and the scenario would then assert the wrong
/// failure.
/// </para>
/// </remarks>
[CompileTime]
public static class Helper
{
    /// <summary>
    /// Gets a message. Nothing consumes it: the consumer must never name a type of <c>Contract</c>, because the type
    /// exists in both assemblies and naming it is <c>CS0433</c>.
    /// </summary>
    public static string Message => "Contract";
}
