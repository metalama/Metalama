// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;

namespace Metalama.Extensions.DependencyInjection.AspectTests.Aspect.Lazy_Struct;

// <target>
public struct TargetStruct()
{
    [Dependency( IsLazy = true )]
    private readonly ILogger _logger;
}