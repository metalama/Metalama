// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.HierarchicalOptions;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Pipeline
{
    public sealed record PipelineContributorSources
    {
        private readonly ImmutableArray<IPipelineContributor> _contributors;

        internal PipelineContributorSources(
            ImmutableArray<IPipelineContributor> contributors,
            IExternalHierarchicalOptionsProvider? externalOptionsProvider = null,
            IExternalAnnotationProvider? externalAnnotationProvider = null )
        {
            // We cannot have duplicates. This means a bug upstream.

            this.Contributors = contributors;
            this.ExternalOptionsProvider = externalOptionsProvider;
            this.ExternalAnnotationProvider = externalAnnotationProvider;
        }

        internal static PipelineContributorSources Empty { get; } = new( ImmutableArray<IPipelineContributor>.Empty );

        public ImmutableArray<IPipelineContributor> Contributors
        {
            get => this._contributors;
            internal init
            {
                Invariant.Assert( value.IsEmpty || value.GroupBy( c => c ).All( c => c.Count() == 1 ) );

                this._contributors = value;
            }
        }

        internal IExternalHierarchicalOptionsProvider? ExternalOptionsProvider { get; }

        internal IExternalAnnotationProvider? ExternalAnnotationProvider { get; }

        internal PipelineContributorSources Add( PipelineContributorSources other )
        {
            return new PipelineContributorSources(
                this.Contributors.AddRange( other.Contributors ),
                this.ExternalOptionsProvider ?? other.ExternalOptionsProvider,
                this.ExternalAnnotationProvider ?? other.ExternalAnnotationProvider );
        }
    }
}