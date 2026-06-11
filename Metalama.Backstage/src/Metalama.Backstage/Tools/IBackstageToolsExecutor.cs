// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;

namespace Metalama.Backstage.Tools;

internal interface IBackstageToolsExecutor : IBackstageService
{
    // The arguments are passed as a vector (not a single string) and are quoted by the implementation, so that
    // untrusted values (e.g. an RSS feed title flowing into a toast notification) cannot inject extra arguments
    // into the child process. See https://github.com/metalama/Metalama/issues/1648.
    IProcess Start( BackstageTool tool, params string[] arguments );
}