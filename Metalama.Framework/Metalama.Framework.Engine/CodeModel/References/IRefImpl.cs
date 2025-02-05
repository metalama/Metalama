// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

// ReSharper disable UnusedMemberInSuper.Global

namespace Metalama.Framework.Engine.CodeModel.References
{
    /// <summary>
    /// A weakly typed base for <see cref="BaseRef{T}"/>.
    /// </summary>
    internal interface IRefImpl : ISdkRef
    {
        IRef ToDurable();
    }
}