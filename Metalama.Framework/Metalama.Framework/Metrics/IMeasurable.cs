// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Metrics
{
    /// <summary>
    /// Base interface for objects that can be extended with metrics.
    /// </summary>
    [InternalImplement]
    [CompileTime]
    public interface IMeasurable;
}