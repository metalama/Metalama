// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.ComponentModel;

namespace Metalama.Framework.Advising;

/// <summary>
/// Indicates the result of applying an advice (code transformation). This enum is returned via <see cref="IAdviceResult.Outcome"/>
/// by all advice methods to indicate whether the transformation succeeded, was ignored, or encountered an error.
/// </summary>
/// <seealso cref="IAdviceResult"/>
/// <seealso cref="AdviserExtensions"/>
/// <seealso cref="IAspectBuilder"/>
/// <seealso cref="OverrideStrategy"/>
/// <seealso href="@advising-code"/>
[CompileTime]
public enum AdviceOutcome
{
    /// <summary>
    /// Synonym to <see cref="Success"/>.
    /// </summary>
    Default,

    /// <summary>
    /// The advice was successfully applied and there was no conflict.
    /// </summary>
    Success = Default,

    /// <summary>
    /// There was a conflict and the advice was successfully applied and the new advice overrides the previous declaration.
    /// </summary>
    Override,

    /// <summary>
    /// There was a conflict and the advice was successfully applied and the new advice hides the previous declaration with the <c>new</c> keyword.
    /// </summary>
    New,

    /// <summary>
    /// The advice was ignored due to a conflict or <see cref="OverrideStrategy"/> settings. Common scenarios include:
    /// a member with the same signature already exists and the <see cref="OverrideStrategy"/> prevents overriding it,
    /// or another aspect with higher priority already introduced the same member.
    /// </summary>
    Ignore,

    [Obsolete]
    [EditorBrowsable( EditorBrowsableState.Never )]
    Ignored = Ignore,

    /// <summary>
    /// An error diagnostic was reported during advice application. The advice was ignored and the whole aspect was automatically skipped.
    /// The <see cref="IAspect{T}.BuildAspect"/> method continues executing without throwing an exception; check <see cref="IAdviceResult.Outcome"/>
    /// to detect errors and handle them appropriately.
    /// </summary>
    Error
}