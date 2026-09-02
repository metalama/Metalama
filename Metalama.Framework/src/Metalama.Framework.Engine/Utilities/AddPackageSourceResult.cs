// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// The outcome of <see cref="NuGetHelper.AddPackageSource"/>.
/// </summary>
/// <param name="IsMappingWritten">
/// Whether a package source mapping was written for the added source.
/// </param>
/// <param name="ConflictingSourceKey">
/// The key of the package source that already maps the pattern, or a more specific one, when that is why no mapping was
/// written, and <c>null</c> otherwise.
/// </param>
/// <param name="ConflictingPattern">
/// The pattern declared by <paramref name="ConflictingSourceKey"/> that caused the decision, and <c>null</c> when no
/// such pattern was found.
/// </param>
/// <remarks>
/// A mapping is skipped silently, so the caller records this outcome in the log file that it writes beside the generated
/// <c>nuget.config</c>, and reports it as the probable cause when the restore then fails on one of the packages covered
/// by the pattern. See issue #1885.
/// </remarks>
internal readonly record struct AddPackageSourceResult( bool IsMappingWritten, string? ConflictingSourceKey, string? ConflictingPattern );
