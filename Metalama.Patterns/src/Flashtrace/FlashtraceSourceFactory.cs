// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Loggers;
using JetBrains.Annotations;

namespace Flashtrace;

[Obsolete( "Use dependency injection." )]
[PublicAPI]
public static class FlashtraceSourceFactory
{
    public static IFlashtraceLoggerFactory DefaultFactory { get; set; } = NullFlashtraceLogger.Instance;

    public static IFlashtraceRoleLoggerFactory Default => DefaultFactory.ForRole( FlashtraceRole.Default );

    public static IFlashtraceRoleLoggerFactory ForRole( FlashtraceRole role ) => DefaultFactory.ForRole( role );
}