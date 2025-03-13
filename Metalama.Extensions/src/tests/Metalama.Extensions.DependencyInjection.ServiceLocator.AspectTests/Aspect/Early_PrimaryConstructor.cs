// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;

namespace Metalama.Extensions.DependencyInjection.DotNet.Tests.Aspect.EarlyOptional.Early_PrimaryConstructor;

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS9113  // Parameter is unread.

// <target>
public class TargetClass( IFormatProvider formatProvider )
{
    [Dependency]
    private readonly ILogger? _logger;

    [Dependency]
    private IFormatProvider _formatProvider;
}