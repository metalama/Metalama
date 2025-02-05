// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;

namespace Metalama.Extensions.DependencyInjection.DotNet.Tests.Aspect.EarlyOptional.Lazy_PrimaryConstructor;

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS9113  // Parameter is unread.

// <target>
public class TargetClass( IFormatProvider formatProvider )
{
    [Dependency( IsLazy = true )]
    private readonly ILogger? _logger;

    [Dependency( IsLazy = true )]
    private IFormatProvider _formatProvider;
}