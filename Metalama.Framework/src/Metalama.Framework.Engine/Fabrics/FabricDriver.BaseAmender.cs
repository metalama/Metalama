// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Queries;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using System;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Fabrics;

internal abstract partial class FabricDriver
{
    protected abstract class BaseAmender<T> : Query<T, int>, IAmender<T>, IQueryOwner
        where T : class, IDeclaration
    {
        // The Target property is protected (and not exposed to the API) because
        private readonly FabricInstance _fabricInstance;

        private IRef<T> TargetDeclaration { get; }

        private readonly FabricManager _fabricManager;
        private readonly IProject _project;
        private readonly UserCodeDescription _userCodeDescription;

        protected BaseAmender(
            IProject project,
            FabricManager fabricManager,
            FabricInstance fabricInstance,
            IRef<T> targetDeclaration,
            UserCodeDescription userCodeDescription ) : base(
            fabricManager.ServiceProvider,
            targetDeclaration,
            CompilationModelVersion.Final,
            ( action, context ) =>
            {
                T target;

                if ( context.Compilation.IsPartial )
                {
                    var targetOrNull = targetDeclaration.GetTargetOrNull( context.Compilation );

                    if ( targetOrNull == null )
                    {
                        // The target declaration may not be resolvable in a partial compilation (design-time scenario).
                        return Task.CompletedTask;
                    }

                    target = targetOrNull;
                }
                else
                {
                    // In complete compilations, fail fast when the target declaration cannot be resolved.
                    target = targetDeclaration.GetTarget( context.Compilation );
                }

                return action( target, 0, context );
            } )
        {
            this._project = project;
            this._fabricInstance = fabricInstance;

            // Only the description of the fabric method is kept, never the execution context it belongs to. An amender
            // belongs to the pipeline configuration, which is long-lived by design, being reused across keystrokes
            // because rebuilding it per keystroke would be prohibitively slow; an execution context is bound to one
            // compilation. Storing one here would therefore make the configuration pin a whole version of the project
            // for the entire editing session, which is the defect reported by issue #1799. The context is built per
            // run instead, by GetUserCodeExecutionContext. The description is a format string and its arguments, and
            // is compilation-neutral.
            this._userCodeDescription = userCodeDescription;

            // What ToDurable costs is now decided by the scope: a batch compilation keeps the reference it is given,
            // because its single compilation outlives the amender. See IDurableRefFactory and issue #1811.
            this.TargetDeclaration = targetDeclaration.ToDurable();
            this._fabricManager = fabricManager;
        }

        protected override bool ShouldCache => false;

        public override IQueryOwner Owner => this;

        IProject IQueryOwner.Project => this._project;

        public abstract string? Namespace { get; }

        ProjectServiceProvider IQueryOwner.ServiceProvider => this._fabricManager.ServiceProvider;

        IAspectClassResolver IQueryOwner.AspectClasses => this._fabricManager.AspectClasses;

        UserCodeInvoker IQueryOwner.UserCodeInvoker => this._fabricManager.UserCodeInvoker;

        public AspectPredecessor AspectPredecessor => new( AspectPredecessorKind.Fabric, this._fabricInstance );

        Type IQueryOwner.Type => this._fabricInstance.Fabric.GetType();

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// Built afresh for each compilation, because a static fabric amender is durable. An amender whose own lifetime
        /// is a single run overrides this and may reuse a context it holds.
        /// </para>
        /// <para>
        /// The context inherits nothing from the ambient one. This method is called on demand rather than while the
        /// fabric is running, so an ambient context here belongs to whoever called into the query, not to the fabric.
        /// See <see cref="UserCodeExecutionContext.CreateWithoutInheritance"/>.
        /// </para>
        /// </remarks>
        public virtual UserCodeExecutionContext GetUserCodeExecutionContext( CompilationModel compilation, IDiagnosticAdder diagnostics )
            => UserCodeExecutionContext.CreateWithoutInheritance( this._fabricManager.ServiceProvider, this._userCodeDescription, compilation, diagnostics );

        [Memo]
        public IQuery<T> Outbound
            => new RootQuery<T>(
                this.TargetDeclaration,
                this,
                CompilationModelVersion.Final );

        string IDiagnosticSource.DiagnosticSourceDescription => $"fabric {this._fabricInstance.Fabric.GetType().FullName}";

        public abstract void AddContributor( IPipelineContributor contributor );
    }
}