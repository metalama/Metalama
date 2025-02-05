// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Project;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Fabrics
{
    /// <summary>
    /// The base class for <see cref="ProjectFabricDriver"/> and <see cref="NamespaceFabricDriver"/>,
    /// which are executed when building the project configuration, not when executing the pipeline.
    /// </summary>
    internal abstract class StaticFabricDriver : FabricDriver
    {
        protected StaticFabricDriver( CreationData creationData ) :
            base( creationData ) { }

        public abstract bool TryExecute(
            IProject project,
            CompilationModel compilation,
            IDiagnosticAdder diagnosticAdder,
            [NotNullWhen( true )] out StaticFabricResult? result );

        protected class StaticAmender<T> : BaseAmender<T>
            where T : class, IDeclaration
        {
            private readonly ImmutableArray<IPipelineContributor>.Builder _contributors = ImmutableArray.CreateBuilder<IPipelineContributor>();

            // TODO PERF: ToDurable is useful only at design time.
            protected StaticAmender(
                IProject project,
                FabricManager fabricManager,
                FabricInstance fabricInstance,
                IRef<T> targetDeclaration,
                string? ns,
                UserCodeExecutionContext userCodeExecutionContext ) :
                base( project, fabricManager, fabricInstance, targetDeclaration.ToDurable(), userCodeExecutionContext )
            {
                this.Namespace = ns;
            }

            public override string? Namespace { get; }

            public override void AddContributor( IPipelineContributor contributor ) => this._contributors.Add( contributor );

            public StaticFabricResult ToResult() => new( this._contributors.ToImmutableArray() );
        }
    }
}