// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Metalama.Framework.Engine.Fabrics
{
    /// <summary>
    /// Implementation of <see cref="FabricAspect{T}"/> for project-level fabrics.
    /// </summary>
    internal sealed class ProjectFabricDriver : StaticFabricDriver
    {
        private readonly int _directoryDepth;
        private readonly (int Min, int Max) _referenceDepth;
        private readonly AssemblyIdentity _containingAssemblyIdentity;

        public static ProjectFabricDriver Create(
            FabricManager fabricManager,
            CompileTimeProject compileTimeProject,
            Fabric fabric,
            Compilation runTimeCompilation )
            => new( GetCreationData( fabricManager, compileTimeProject, fabric, runTimeCompilation ) );

        private ProjectFabricDriver( CreationData creationData ) :
            base( creationData )
        {
            var depth = 0;

            // Compute the distance of the path to the root.
            for ( var path = this.OriginalPath; !string.IsNullOrEmpty( path ); path = Path.GetDirectoryName( path ) )
            {
                depth++;
            }

            this._directoryDepth = depth;

            this._containingAssemblyIdentity = creationData.FabricType.ContainingAssembly.Identity;
            this._referenceDepth = CompilationReferenceGraph.GetInstance( creationData.Compilation ).GetDepth( creationData.FabricType.ContainingAssembly );
        }

        public override FabricKind Kind => this.Fabric is TransitiveProjectFabric ? FabricKind.Transitive : FabricKind.Compilation;

        protected override int CompareToCore( FabricDriver other )
        {
            var otherProjectFabricDriver = (ProjectFabricDriver) other;

            if ( this.Fabric is TransitiveProjectFabric )
            {
                // For transitive project fabrics, we first consider the depth of the dependency in the project graph.

                var referenceDepthComparison = this._referenceDepth.Max.CompareTo( otherProjectFabricDriver._referenceDepth.Max );

                if ( referenceDepthComparison != 0 )
                {
                    return referenceDepthComparison;
                }

                // Then we sort by assembly name, to make sure all fabrics of the same project run together.
                var nameComparison = string.Compare(
                    this._containingAssemblyIdentity.Name,
                    otherProjectFabricDriver._containingAssemblyIdentity.Name,
                    StringComparison.OrdinalIgnoreCase );

                if ( nameComparison != 0 )
                {
                    return nameComparison;
                }
            }

            // Compare by depth from the root directory.
            var depthComparison = this._directoryDepth.CompareTo( otherProjectFabricDriver._directoryDepth );

            if ( depthComparison != 0 )
            {
                return depthComparison;
            }

            // Finally compare by type name, ignoring the namespace.
            return string.Compare( this.FabricTypeShortName, other.FabricTypeShortName, StringComparison.Ordinal );
        }

        public override FormattableString FormatPredecessor() => $"project fabric '{this.Fabric.GetType()}'";

        public override bool TryExecute(
            IProject project,
            CompilationModel compilation,
            IDiagnosticAdder diagnosticAdder,
            [NotNullWhen( true )] out StaticFabricResult? result )
        {
            var assembly = compilation.Factory.GetAssembly( this._containingAssemblyIdentity );

            var projectFabric = (ProjectFabric) this.Fabric;

            var executionContext = UserCodeExecutionContext.CreateInstance(
                this.FabricManager.ServiceProvider.Underlying,
                UserCodeDescription.Create( "calling the AmendProject method for the fabric {0}", projectFabric.GetType() ),
                compilation,
                diagnostics: diagnosticAdder );

            var amender = new Amender(
                project,
                compilation,
                this.FabricManager,
                new FabricInstance( this, assembly ),
                executionContext );

            if ( !this.FabricManager.UserCodeInvoker.TryInvoke( () => projectFabric.AmendProject( amender ), executionContext ) )
            {
                result = null;

                return false;
            }

            result = amender.ToResult();

            return true;
        }

        private sealed class Amender : StaticAmender<ICompilation>, IProjectAmender
        {
            public Amender(
                IProject project,
                CompilationModel compilation,
                FabricManager fabricManager,
                FabricInstance fabricInstance,
                UserCodeExecutionContext userCodeExecutionContext ) : base(
                project,
                fabricManager,
                fabricInstance,
                compilation.RefFactory.ForCompilation(),
                null,
                userCodeExecutionContext ) { }
        }
    }
}