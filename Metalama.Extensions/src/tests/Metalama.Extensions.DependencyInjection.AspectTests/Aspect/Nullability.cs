// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.Extensions.Logging;

namespace Metalama.Extensions.DependencyInjection.AspectTests.Aspect.Nullability;

public class C
{
    // Optional.
    [Dependency]
    private ILoggerFactory? _loggerFactory;

    // Required.
    [Dependency]
    private IFormatProvider _formatProvider;
}