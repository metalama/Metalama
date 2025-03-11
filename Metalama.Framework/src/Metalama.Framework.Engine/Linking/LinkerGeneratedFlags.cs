// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Linking;

[Flags]
internal enum LinkerGeneratedFlags
{
    // Resharper disable UnusedMember.Global
    None = 0,

    /// <summary>
    /// Block that was added by the linker but can be flattened.
    /// </summary>
    FlattenableBlock = 1,

    /// <summary>
    /// Labeled empty statement added by the linker and can be attached to the next statement.
    /// </summary>
    EmptyLabeledStatement = 2,

    /// <summary>
    /// Empty statement that was added by the linker to carry trivia, which should be attached to surrounding statements.
    /// </summary>
    EmptyTriviaStatement = 4,

    /// <summary>
    /// Null literal expression that replaced the original aspect reference. 
    /// It's parent statement should be removed and parent expression should be converted to "default".
    /// </summary>
    NullAspectReferenceExpression = 8,

    /// <summary>
    /// Generated suppression, which should be removed when inlined.
    /// </summary>
    GeneratedSuppression = 16
}