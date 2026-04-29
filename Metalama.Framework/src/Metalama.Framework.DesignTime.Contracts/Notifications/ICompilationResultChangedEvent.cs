// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Notifications;

/// <summary>
/// Event raised when the compilation result of a project changes.
/// </summary>
/// <remarks>
/// Cross-version contract. <see cref="GuidAttribute"/>, type name, and member signatures are frozen forever.
/// </remarks>
[ComImport]
[Guid( "1C09E6CD-AF9C-4DD3-A431-0B5F3A59F77A" )]
public interface ICompilationResultChangedEvent : IDesignTimeNotificationEvent
{
    /// <summary>
    /// Gets the string form of the project key whose compilation result changed.
    /// </summary>
    string ProjectKey { get; }

    /// <summary>
    /// Gets a value indicating whether the change affects only specific syntax trees (<c>true</c>) or the whole compilation (<c>false</c>).
    /// </summary>
    bool IsPartialCompilation { get; }

    /// <summary>
    /// Gets the file paths of syntax trees affected by the change. When <see cref="IsPartialCompilation"/> is <c>false</c>,
    /// this array may be empty and subscribers should treat the whole project as invalidated.
    /// </summary>
    string[] SyntaxTreePaths { get; }
}
