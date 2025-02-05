// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Engine.Options;

public interface IGlobalOptions : IGlobalService
{
    TimeSpan QuietPeriodTimerDelay { get; }
}