// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Activities;
using JetBrains.Annotations;

namespace Flashtrace.Options;

/// <summary>
/// Options of a new <see cref="LogActivity{TActivityDescription}"/>.
/// </summary>
/// <param name="Kind">
/// The kind of <see cref="LogActivity{TActivityDescription}"/>.
/// </param>
/// Indicates whether the <see cref="LogActivityOptions"/> is async.
/// <param name="IsAsync">
/// </param>
[PublicAPI]
public readonly record struct LogActivityOptions( LogActivityKind Kind, bool IsAsync );