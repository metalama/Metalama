// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// Exposes <see cref="LanguageVersion"/> regardless of the version of Roslyn we are compiling with.
/// </summary>
internal static class AllLanguageVersions
{
    public const LanguageVersion CSharp10 = (LanguageVersion) 1000;
    public const LanguageVersion CSharp11 = (LanguageVersion) 1100;
    public const LanguageVersion CSharp12 = (LanguageVersion) 1200;
    public const LanguageVersion CSharp13 = (LanguageVersion) 1300;
    public const LanguageVersion CSharp14 = (LanguageVersion) 1400;
}