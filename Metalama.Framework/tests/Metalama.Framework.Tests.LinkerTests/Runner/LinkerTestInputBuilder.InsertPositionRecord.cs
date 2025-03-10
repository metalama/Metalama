// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Transformations;

// ReSharper disable SuspiciousTypeConversion.Global

namespace Metalama.Framework.Tests.LinkerTests.Runner
{
    internal partial class LinkerTestInputBuilder
    {
        internal sealed class InsertPositionRecord
        {
            /// <summary>
            /// The relation of the insertion.
            /// </summary>
            public InsertPositionRelation Relation { get; }

            /// <summary>
            /// The node ID of the node that is the target of the insertion in case this targets a source declaration.
            /// </summary>
            public string? NodeId { get; }

            public InsertPositionRecord( InsertPositionRelation relation, string nodeId )
            {
                this.NodeId = nodeId;
                this.Relation = relation;
            }
        }
    }
}