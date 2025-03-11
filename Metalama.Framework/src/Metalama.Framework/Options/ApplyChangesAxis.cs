// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Options;

/// <summary>
/// Enumerates the axes along which two option layers can be merged by the <see cref="IIncrementalObject.ApplyChanges"/> method. 
/// </summary>
[CompileTime]
public enum ApplyChangesAxis
{
    /// <summary>
    /// Means that options directly applied to the declaration override other options also directly applied to the declaration.
    /// </summary>
    SameDeclaration,

    /// <summary>
    /// Means that options on the containing declaration (typically the declaring type, but not the namespace, which are specified by the
    /// <see cref="BaseDeclaration"/> axis) override the options defined in the base declaration.
    /// For instance, type-level options on the declaring type of an <c>override</c> method override method-level options on the <c>base</c> method.
    /// </summary>
    ContainingDeclaration,

    /// <summary>
    /// Means that options on the base type or overridden member override the options inherited from the namespace or the default options.
    /// </summary>
    BaseDeclaration,

    /// <summary>
    /// Means that options directly applied to the declaration override options inherited along the containment or base axis.
    /// </summary>
    TargetDeclaration,

    /// <summary>
    /// Means that options defined by the aspect instance itself override any other option.
    /// </summary>
    Aspect
}