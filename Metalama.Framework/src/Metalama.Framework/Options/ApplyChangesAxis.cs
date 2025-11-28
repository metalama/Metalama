// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Options;

/// <summary>
/// Enumerates the axes along which two option layers can be merged by the <see cref="IIncrementalObject.ApplyChanges"/> method.
/// Understanding these axes is important when customizing how options are inherited and merged across different declaration levels.
/// </summary>
/// <remarks>
/// <para>
/// When options are resolved for a declaration, Metalama merges options from multiple sources in a specific order. Each merging step
/// occurs along one of these axes, which determines the precedence and priority of the options being combined.
/// </para>
/// <para>
/// The general priority order (from lowest to highest precedence) is: default options, namespace options, base type/member options,
/// containing type options, and finally options on the target declaration itself. Options provided by aspects have the highest priority.
/// </para>
/// <para>
/// Aspect authors can disable inheritance along specific axes by using the <see cref="HierarchicalOptionsAttribute"/> on their
/// option class.
/// </para>
/// </remarks>
/// <seealso cref="IIncrementalObject.ApplyChanges"/>
/// <seealso cref="HierarchicalOptionsAttribute"/>
/// <seealso href="@configuration-custom-merge"/>
[CompileTime]
public enum ApplyChangesAxis
{
    /// <summary>
    /// Indicates that options from multiple sources (custom attributes, fabrics, aspects) are being merged for the same declaration.
    /// This axis is used when combining options defined at the same declaration level before inheritance rules apply.
    /// </summary>
    SameDeclaration,

    /// <summary>
    /// Indicates that options from a containing declaration (such as a declaring type) are being applied to a nested declaration (such as a member).
    /// This also applies when namespace-level options override default project options. For instance, type-level options on the declaring
    /// type of an <c>override</c> method override method-level options inherited from the <c>base</c> method.
    /// </summary>
    ContainingDeclaration,

    /// <summary>
    /// Indicates that options from a base type or overridden member are being applied to a derived type or overriding member.
    /// Options along this axis have lower priority than namespace-level options and default options from the base project.
    /// </summary>
    BaseDeclaration,

    /// <summary>
    /// Indicates that options directly applied to the target declaration are overriding options inherited from containing
    /// declarations, base declarations, or default options. This axis represents the final application of declaration-specific options.
    /// </summary>
    TargetDeclaration,

    /// <summary>
    /// Indicates that options provided by an aspect instance (via <see cref="IHierarchicalOptionsProvider"/>) are being applied.
    /// Options along this axis have the highest priority and override all other options.
    /// </summary>
    Aspect
}