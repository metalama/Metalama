// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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