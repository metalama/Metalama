// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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