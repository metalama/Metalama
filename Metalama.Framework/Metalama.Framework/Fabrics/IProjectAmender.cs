// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// Argument of <see cref="ProjectFabric.AmendProject"/>. Allows reporting diagnostics and adding aspects to the target project. 
    /// </summary>
    public interface IProjectAmender : IAmender<ICompilation>;
}