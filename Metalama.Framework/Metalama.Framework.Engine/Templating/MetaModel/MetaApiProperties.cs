// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;

namespace Metalama.Framework.Engine.Templating.MetaModel
{
    /// <summary>
    /// Encapsulates properties that are common to all constructors of <see cref="Metalama.Framework.Engine.Templating.MetaModel.MetaApi"/>.
    /// </summary>
    internal sealed class MetaApiProperties
    {
        public UserDiagnosticSink DiagnosticSink { get; }

        public TemplateMember<IMemberOrNamedType> Template { get; }

        public IObjectReader Tags => this.Template.Tags;

        public AspectLayerId AspectLayerId { get; }

        public SyntaxGenerationContext SyntaxGenerationContext { get; }

        public IAspectInstanceInternal? AspectInstance { get; }

        public ProjectServiceProvider ServiceProvider { get; }

        public MetaApiStaticity Staticity { get; }

        private ICompilation SourceCompilation { get; }

        public MetaApiProperties(
            ICompilation sourceCompilation,
            UserDiagnosticSink diagnosticSink,
            TemplateMember<IMemberOrNamedType> template,
            AspectLayerId aspectLayerId,
            SyntaxGenerationContext syntaxGenerationContext,
            IAspectInstanceInternal? aspectInstance, // Can be null in tests.
            ProjectServiceProvider serviceProvider,
            MetaApiStaticity staticity )
        {
            this.SourceCompilation = sourceCompilation;
            this.DiagnosticSink = diagnosticSink;
            this.Template = template;
            this.AspectLayerId = aspectLayerId;
            this.SyntaxGenerationContext = syntaxGenerationContext;
            this.AspectInstance = aspectInstance;
            this.ServiceProvider = serviceProvider;
            this.Staticity = staticity;
        }

        internal T Translate<T>( T declaration )
            where T : class, IDeclaration
            => declaration.ForCompilation( this.SourceCompilation );
    }
}