// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.UserInterface.Toasts;

/// <summary>
/// Categories of toast notifications, for <see cref="IToastNotificationDetectionService"/>.
/// </summary>
[Flags]
public enum ToastNotificationCategories
{
    None = 0,

    /// <summary>
    /// Notifications typically displayed by the licensing process.
    /// </summary>
    Licensing = 1,

    /// <summary>
    /// Notifications typically displayed by the compiler process.
    /// </summary>
    Compiler = 2,

    /// <summary>
    /// All notification categories (typically used by tests and by callers that want to detect every category).
    /// </summary>
    All = Licensing | Compiler
}