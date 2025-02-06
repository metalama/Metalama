// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Activities;

internal readonly record struct ActivityLevels( FlashtraceLevel DefaultLevel, FlashtraceLevel FailureLevel );