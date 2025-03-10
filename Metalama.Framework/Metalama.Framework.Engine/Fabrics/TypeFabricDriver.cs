// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Fabrics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Fabrics;

/// <summary>
/// Implementation of <see cref="FabricAspect{T}"/> for type-level fabrics.
/// </summary>
internal sealed class TypeFabricDriver : FabricDriver
{
    private readonly string _targetTypeFullName;

    private TypeFabricDriver( CreationData creationData, INamedTypeSymbol targetType ) : base( creationData )
    {
        this._targetTypeFullName = targetType.AssertSymbolNotNull().GetFullName().AssertNotNull();
    }

    public static IEnumerable<TypeFabricDriver> Create(
        FabricManager fabricManager,
        CompileTimeProject compileTimeProject,
        Fabric fabric,
        CompilationModel compilation )
    {
        var creationData = GetCreationData( fabricManager, compileTimeProject, fabric, compilation.RoslynCompilation );

        if ( creationData.FabricType.ContainingAssembly.Equals( compilation.RoslynCompilation.Assembly ) )
        {
            yield return new TypeFabricDriver( creationData, creationData.FabricType.ContainingType );
        }

        if ( creationData.FabricType.GetAttributes().Any( a => a.AttributeClass?.Name == nameof(InheritableAttribute) ) )
        {
            foreach ( var derivedType in compilation.GetDerivedTypes( compilation.Factory.GetNamedType( creationData.FabricType.ContainingType ) ) )
            {
                yield return new TypeFabricDriver(
                    creationData,
                    derivedType.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.DerivedFabricsOnIntroducedTypes ) );
            }
        }
    }

    public bool TryExecute( IAspectBuilderInternal aspectBuilder, FabricTemplateClass templateClass, FabricInstance fabricInstance )
    {
        var templateProvider = TemplateProvider.FromInstance( this.Fabric );
        var templateInstance = new TemplateClassInstance( templateProvider, templateClass );
        var targetType = (INamedType) aspectBuilder.Target;
        var compilation = aspectBuilder.Target.GetCompilationModel();

        // Prepare declarative advice.
        var declarativeAdvice = templateClass
            .GetDeclarativeAdvice( aspectBuilder.ServiceProvider, compilation, templateProvider, ObjectReader.Empty )
            .ToReadOnlyList();

        var executionContext = UserCodeExecutionContext.CreateInstance(
            aspectBuilder.ServiceProvider,
            UserCodeDescription.Create( "calling the AmendType method for the fabric {0}", this.Fabric.GetType() ),
            compilation,
            diagnostics: aspectBuilder.DiagnosticAdder );

        // Execute the AmendType.
        var builder = new Amender( targetType, this.FabricManager, aspectBuilder, templateInstance, fabricInstance, executionContext );

        return this.FabricManager.UserCodeInvoker.TryInvoke(
            () =>
            {
                // Execute declarative advice.
                foreach ( var advice in declarativeAdvice )
                {
                    ((DeclarativeAdviceAttribute) advice.AdviceAttribute.AssertNotNull()).BuildAdvice(
                        advice.GetDeclaration( compilation ),
                        advice.TemplateClassMember.Key,
                        (IAspectBuilder<IDeclaration>) aspectBuilder );
                }

                if ( !aspectBuilder.IsAspectSkipped )
                {
                    ((TypeFabric) this.Fabric).AmendType( builder );
                }
            },
            executionContext );
    }

    public override FabricKind Kind => FabricKind.Type;

    public IDeclaration? GetTargetIfInPartialCompilation( CompilationModel compilation )
    {
        var symbol = compilation.RoslynCompilation.GetTypeByMetadataName( this._targetTypeFullName );

        if ( symbol == null || (compilation.PartialCompilation.IsPartial && !compilation.PartialCompilation.Types.Contains( symbol )) )
        {
            return null;
        }

        return compilation.Factory.GetNamedType( symbol );
    }

    public override FormattableString FormatPredecessor() => $"type fabric on '{this._targetTypeFullName}'";

    private sealed class Amender : BaseAmender<INamedType>, ITypeAmender
    {
        private readonly IAspectBuilderInternal _aspectBuilder;

        public Amender(
            INamedType namedType,
            FabricManager fabricManager,
            IAspectBuilderInternal aspectBuilder,
            TemplateClassInstance templateClassInstance,
            FabricInstance fabricInstance,
            UserCodeExecutionContext userCodeExecutionContext ) : base(
            namedType.Compilation.Project,
            fabricManager,
            fabricInstance,
            fabricInstance.TargetDeclaration.As<INamedType>(),
            userCodeExecutionContext )
        {
            this._aspectBuilder = aspectBuilder;
            this.Type = namedType;
            this.Advice = ((IAdviceFactoryImpl) aspectBuilder.AdviceFactory).WithTemplateClassInstance( templateClassInstance );
        }

        public INamedType Type { get; }

        public override void AddContributor( IPipelineContributor contributor ) => this._aspectBuilder.AddContributor( contributor );

        public IAdviceFactory Advice { get; }

        public override string Namespace => this.Type.ContainingNamespace.FullName;
    }
}