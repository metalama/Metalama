// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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