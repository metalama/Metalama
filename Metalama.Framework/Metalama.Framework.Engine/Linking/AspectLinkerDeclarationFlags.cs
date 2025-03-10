// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Declaration flags used internally by the aspect linker.
/// </summary>
[Flags]
internal enum AspectLinkerDeclarationFlags
{
    None = 0,

    /// <summary>
    /// Used to denote event field declaration where event field declaration is not possible (e.g. explicit interface implementation with event field template).
    /// </summary>
    EventField = 1,

    /// <summary>
    /// Used to denote that the declaration has an initializer expression that is hidden (depends on the declaration type).
    /// </summary>
    HasHiddenInitializerExpression = 2,

    /// <summary>
    /// Used to denote that the declaration has a default initializer expression.
    /// </summary>
    HasDefaultInitializerExpression = 4,

    /// <summary>
    /// Mask for determining presence of any initializer expression.
    /// </summary>
    HasInitializerExpressionMask = HasHiddenInitializerExpression | HasDefaultInitializerExpression,

    /// <summary>
    /// Used to denote a declaration which should not be inlined into. Used for abstract/virtual properties that have pseudo setter.
    /// </summary>
    NotInliningDestination = 1 << 14,

    /// <summary>
    /// Used to denote a declaration body of which should not be inlined by the linker.
    /// </summary>
    NotInlineable = 1 << 15,

    /// <summary>
    /// User to denote a declaration which should not be discarded. 
    /// </summary>
    NotDiscardable = 1 << 16
}