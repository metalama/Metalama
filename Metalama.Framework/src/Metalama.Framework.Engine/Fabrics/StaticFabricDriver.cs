// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Project;
using System;
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
            private readonly UserCodeDescription _userCodeDescription;
            private bool _isCompleted;

            // The representation of the durable reference depends on the execution scenario. During a batch
            // compilation, ToDurable stores the given reference, because the compilation lives longer than the amender.
            // See IDurableRefFactory and issue #1811.
            protected StaticAmender(
                IProject project,
                FabricManager fabricManager,
                FabricInstance fabricInstance,
                IRef<T> targetDeclaration,
                string? ns,
                UserCodeDescription userCodeDescription ) :
                base( project, fabricManager, fabricInstance, targetDeclaration.ToDurable(), userCodeDescription )
            {
                this.Namespace = ns;
                this._userCodeDescription = userCodeDescription;
            }

            public override string? Namespace { get; }

            /// <summary>
            /// Adds a pipeline contributor to the result of the fabric.
            /// </summary>
            /// <remarks>
            /// The method throws after <see cref="Complete"/> has been called, because the contributor would not be part of the result.
            /// This happens when a fabric stores the amender, or an object that the amender created, and uses it after the method
            /// of the fabric has returned, typically in the execution context of an aspect instance.
            /// </remarks>
            public override void AddContributor( IPipelineContributor contributor )
            {
                if ( this._isCompleted )
                {
                    var currentContext = UserCodeExecutionContext.CurrentOrNull?.Description.ToString();

                    var usage = currentContext != null
                        ? $"cannot be used while {currentContext}"
                        : "cannot be used outside of an execution context";

                    throw new InvalidOperationException(
                        $"The amender of a fabric, or an object that it created, belongs to a different execution context and {usage}. "
                        + $"It can be used only while {this._userCodeDescription}." );
                }

                this._contributors.Add( contributor );
            }

            /// <summary>
            /// Marks the execution of the fabric as completed, so that <see cref="AddContributor"/> throws from now on.
            /// </summary>
            /// <remarks>
            /// The fabric driver calls this method on every exit from the method of the fabric, including when this method throws,
            /// because a query created before the exception can still be used afterwards.
            /// </remarks>
            public void Complete() => this._isCompleted = true;

            public StaticFabricResult ToResult()
            {
                this.Complete();

                return new StaticFabricResult( this._contributors.ToImmutableArray() );
            }
        }
    }
}