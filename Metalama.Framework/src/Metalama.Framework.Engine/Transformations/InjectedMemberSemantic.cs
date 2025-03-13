// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Transformations;

internal enum InjectedMemberSemantic
{
    /// <summary>
    /// The injected member is an introduction of a new/replaced declaration.
    /// </summary>
    Introduction,

    /// <summary>
    /// The injected member is an override of another declaration.
    /// </summary>
    Override,

    /// <summary>
    /// The injected member is a container for initializer expression of another declaration.
    /// </summary>
    InitializerMethod,

    /// <summary>
    /// The injected member is an auxiliary body with a trivial structure that is meant to receive other transformations (e.g. inserted statements).
    /// </summary>
    AuxiliaryBody
}