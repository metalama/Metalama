// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Templating.MetaModel
{
    /// <summary>
    /// Defines staticity of the meta api.
    /// </summary>
    internal enum MetaApiStaticity
    {
        /// <summary>
        /// Staticity of meta api depends on the context.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Staticity of meta api is always static.
        /// </summary>
        AlwaysStatic = 1,

        /// <summary>
        /// Staticity of meta api is always instance.
        /// </summary>
        AlwaysInstance = 2
    }
}