// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    private sealed class ReturnStatementProperties
    {
        /// <summary>
        /// Gets a value indicating whether the control would flow to the method exit if the return statement was replaced.
        /// </summary>
        public bool FlowsToExitIfRewritten { get; }

        /// <summary>
        /// Gets a value indicating whether the return statement needs to be rewritten to break statement.
        /// </summary>
        public bool ReplaceWithBreakIfOmitted { get; }

        public ReturnStatementProperties( bool flowsToExitIfRewritten, bool replaceWithBreakIfOmitted )
        {
            this.FlowsToExitIfRewritten = flowsToExitIfRewritten;
            this.ReplaceWithBreakIfOmitted = replaceWithBreakIfOmitted;
        }
    }
}