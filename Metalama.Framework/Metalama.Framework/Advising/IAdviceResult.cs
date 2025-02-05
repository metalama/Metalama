// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Advising;

/* The benefits of the design of having each advice kind return its own IAdviceResult interface are that:
    - it is possible to build fluent APIs based on advice results
    - it is possible to extend the interfaces with more properties
*/

/// <summary>
/// A base interface that represents the result of any advice method of the <see cref="IAdviceFactory"/> interface.
/// </summary>
[CompileTime]
[InternalImplement]
public interface IAdviceResult
{
    /// <summary>
    /// Gets the kind of advice whose the current object is the result.
    /// </summary>
    AdviceKind AdviceKind { get; }

    /// <summary>
    /// Gets the advice outcome, i.e. indication whether the advice was applied, was ignored because the same declaration already exists (according to <see cref="OverrideStrategy"/>),
    /// or an error for different reasons. 
    /// </summary>
    AdviceOutcome Outcome { get; }
}