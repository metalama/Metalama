// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Aspects;

/// <summary>
/// Specifies the order in which the aspect types or aspect layers are supplied to  <see cref="AspectOrderAttribute"/>. 
/// </summary>
public enum AspectOrderDirection
{
    /// <summary>
    /// Means that the <see cref="AspectOrderAttribute"/>'s parameter specifies the run-time execution order, which is more intuitive to aspect users.
    /// Prior to Metalama 2024.2, this value was the only possible one.
    /// </summary>
    RunTime,

    /// <summary>
    /// Means that the <see cref="AspectOrderAttribute"/>'s parameter specifies the compile-time execution order (i.e. the order in which
    /// the aspects are executed in Metalama), which is intuitive to aspect authors.
    /// </summary>
    CompileTime
}