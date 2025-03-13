// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Queries;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using System;

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

        protected BaseAmender(
            IProject project,
            FabricManager fabricManager,
            FabricInstance fabricInstance,
            IRef<T> targetDeclaration,
            UserCodeExecutionContext userCodeExecutionContext ) : base(
            fabricManager.ServiceProvider,
            targetDeclaration,
            CompilationModelVersion.Final,
            ( action, context ) => action( targetDeclaration.GetTarget( context.Compilation ), 0, context ) )
        {
            this._project = project;
            this._fabricInstance = fabricInstance;
            this.UserCodeExecutionContext = userCodeExecutionContext;
            this.TargetDeclaration = targetDeclaration.ToDurable(); // TODO PERF: ToDurable is useful only at design time.
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

        public UserCodeExecutionContext UserCodeExecutionContext { get; }

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