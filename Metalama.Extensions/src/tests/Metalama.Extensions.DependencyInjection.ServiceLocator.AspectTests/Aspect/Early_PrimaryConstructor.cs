// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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