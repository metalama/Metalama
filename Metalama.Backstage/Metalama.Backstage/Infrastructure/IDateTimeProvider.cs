// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Infrastructure
{
    public interface IDateTimeProvider : IBackstageService
    {
        DateTime UtcNow { get; }
    }
}