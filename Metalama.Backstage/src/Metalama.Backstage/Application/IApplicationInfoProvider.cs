// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Application
{
    // TODO: For licensing, we need info about all applications together.
    // TODO: Split IApplicationInfo to application, component and process info.
    public interface IApplicationInfoProvider : IBackstageService
    {
        IApplicationInfo CurrentApplication { get; }
    }
}