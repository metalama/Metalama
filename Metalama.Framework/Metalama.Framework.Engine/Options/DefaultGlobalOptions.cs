// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Engine.Options;

[PublicAPI]
public class DefaultGlobalOptions : IGlobalOptions
{
    public virtual TimeSpan QuietPeriodTimerDelay => TimeSpan.FromSeconds( 3 );
}