// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Framework.Engine.Utilities;

public interface ILanguageVersionProvider : IProjectService
{
    /// <summary>
    /// Gets the highest C# language version supported when compiling the template,
    /// which depends on the SDK and not on the Roslyn version of the current process.
    /// </summary>
    /// <returns></returns>
    LanguageVersion GetCompileTimeLanguageVersion();
}