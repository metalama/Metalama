// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.AspectExplorer;
using Metalama.Framework.DesignTime.CodeLens;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;
using Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using Metalama.Framework.Engine.DesignTime;

namespace Metalama.Framework.DesignTime.VisualStudio.Rpc;

internal sealed class JsonSerializationBinderProvider : IJsonSerializationBinderProvider
{
    public JsonSerializationBinder Binder { get; } = new(
        configuration =>
        {
            configuration.AddAssemblyOfType( typeof(IAspect) );

            // SECURITY (#1651 / GHSA-h26j-4vp7-x9w2): allow-list the DesignTime / Engine / core DTO types that ride the
            // JSON wire (the Rpc-assembly types and collections are registered by the JsonSerializationBinder ctor).
            // This is the per-type complement to the assembly-level registrations above; under TypeNameHandling.All every
            // one of these is named on the wire and resolved through BindToType.

            // Metalama.Framework.DesignTime DTOs.
            configuration.AddType( typeof(AspectDatabaseAspectInstance) );
            configuration.AddType( typeof(AspectDatabaseAspectTransformation) );
            configuration.AddType( typeof(AspectClassesChangedEventData) );
            configuration.AddType( typeof(AspectInstancesChangedEventData) );
            configuration.AddType( typeof(CodeLensSummary) );
            configuration.AddType( typeof(CodeLensDetailsTable) );
            configuration.AddType( typeof(CodeLensDetailsHeader) );
            configuration.AddType( typeof(CodeLensDetailsEntry) );
            configuration.AddType( typeof(CodeLensDetailsField) );
            configuration.AddType( typeof(DiagnosticData) );
            configuration.AddType( typeof(CompileTimeEditingStatusChangedEventData) );
            configuration.AddType( typeof(CompileTimeErrorsChangedEventData) );
            configuration.AddType( typeof(RpcServiceInfo) );
            configuration.AddType( typeof(ServicesAddedEventData) );
            configuration.AddType( typeof(GeneratedSourceChangedEventData) );
            configuration.AddType( typeof(SerializablePreviewTransformationResult) );

            // Metalama.Framework.Engine DTOs.
            configuration.AddType( typeof(SerializableSyntaxTree) );
            configuration.AddType( typeof(SerializableAnnotation) );

            // Metalama.Framework (core) DTOs. SerializableDeclarationId is a struct serialized as an object (it appears as
            // a CodeLens RPC parameter); SerializableTypeId only ever crosses the wire in its string form, so it is not listed.
            configuration.AddType( typeof(SerializableDeclarationId) );

#if ROSLYN_4_8_0
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.DesignTime.4.8.0" );
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.Engine.4.8.0" );
#endif

#if ROSLYN_4_12_0
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.DesignTime.4.12.0" );
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.Engine.4.12.0" );
#endif
        } );
}