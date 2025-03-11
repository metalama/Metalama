// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace;

/// <summary>
/// Creates instances of <see cref="IFlashtraceRoleLoggerFactory"/> for a specified role.
/// </summary>
[PublicAPI]
public interface IFlashtraceLoggerFactory
{
    /// <summary>
    /// Gets an instance of the <see cref="IFlashtraceRoleLoggerFactory"/> interface.
    /// </summary>
    /// <param name="role">The role for which the logger is requested.</param>
    /// <returns></returns>
    IFlashtraceRoleLoggerFactory ForRole( FlashtraceRole role );
}