// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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