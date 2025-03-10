// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Options
{
    /// <summary>
    /// Gives access to configuration options (typically values pulled from MSBuild). The
    /// typical implementation wraps a <see cref="AnalyzerConfigOptions"/>, but other implementations can be used for testing.
    /// </summary>
    public interface IProjectOptionsSource
    {
        /// <summary>
        /// Gets a configuration value.
        /// </summary>
        bool TryGetValue( string name, out string? value );

        /// <summary>
        /// Gets a collection of all known configuration names.
        /// </summary>
        public IEnumerable<string> PropertyNames { get; }
    }
}