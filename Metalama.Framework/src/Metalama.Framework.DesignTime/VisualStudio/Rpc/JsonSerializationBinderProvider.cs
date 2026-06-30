// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.Rpc;

internal sealed class JsonSerializationBinderProvider : IJsonSerializationBinderProvider
{
    public JsonSerializationBinder Binder { get; } = new(
        configuration =>
        {
            configuration.AddAssemblyOfType( typeof(IAspect) );

            // SECURITY (#1651 / GHSA-h26j-4vp7-x9w2): allow-list the DesignTime / Engine / core DTO types that ride the
            // JSON wire (the Rpc-assembly types and collections are registered by the JsonSerializationBinder ctor).
            // Under TypeNameHandling.All every one of these is named on the wire and resolved through BindToType.
            //
            // These are registered by full NAME rather than via typeof. Several of them implement a shared
            // Metalama.Framework.DesignTime.Contracts interface (e.g. DiagnosticData : IDiagnosticData, CodeLensSummary :
            // ICodeLensSummary), and force-loading such a type during binder construction throws a TypeLoadException in
            // the multi-version design-time scenario (#31075) when a different version of the Contracts assembly is already
            // loaded in the process. The allow-list matches by full name only, so no assembly identity is needed here.

            // Metalama.Framework.DesignTime DTOs.
            configuration.AddType( "Metalama.Framework.DesignTime.AspectExplorer.AspectDatabaseAspectInstance" );
            configuration.AddType( "Metalama.Framework.DesignTime.AspectExplorer.AspectDatabaseAspectTransformation" );
            configuration.AddType( "Metalama.Framework.DesignTime.VisualStudio.AspectExplorer.AspectClassesChangedEventData" );
            configuration.AddType( "Metalama.Framework.DesignTime.VisualStudio.AspectExplorer.AspectInstancesChangedEventData" );
            configuration.AddType( "Metalama.Framework.DesignTime.CodeLens.CodeLensSummary" );
            configuration.AddType( "Metalama.Framework.DesignTime.CodeLens.CodeLensDetailsTable" );
            configuration.AddType( "Metalama.Framework.DesignTime.CodeLens.CodeLensDetailsHeader" );
            configuration.AddType( "Metalama.Framework.DesignTime.CodeLens.CodeLensDetailsEntry" );
            configuration.AddType( "Metalama.Framework.DesignTime.CodeLens.CodeLensDetailsField" );
            configuration.AddType( "Metalama.Framework.DesignTime.Diagnostics.DiagnosticData" );
            configuration.AddType( "Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus.CompileTimeEditingStatusChangedEventData" );
            configuration.AddType( "Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus.CompileTimeErrorsChangedEventData" );
            configuration.AddType( "Metalama.Framework.DesignTime.VisualStudio.ServiceProvider.RpcServiceInfo" );
            configuration.AddType( "Metalama.Framework.DesignTime.VisualStudio.ServiceProvider.ServicesAddedEventData" );
            configuration.AddType( "Metalama.Framework.DesignTime.VisualStudio.SourceGenerating.GeneratedSourceChangedEventData" );
            configuration.AddType( "Metalama.Framework.DesignTime.Preview.SerializablePreviewTransformationResult" );

            // Metalama.Framework.Engine DTOs.
            configuration.AddType( "Metalama.Framework.Engine.DesignTime.SerializableSyntaxTree" );
            configuration.AddType( "Metalama.Framework.Engine.DesignTime.SerializableAnnotation" );

            // Metalama.Framework (core) DTOs. SerializableDeclarationId is a struct serialized as an object (it appears as
            // a CodeLens RPC parameter); SerializableTypeId only ever crosses the wire in its string form, so it is not listed.
            configuration.AddType( "Metalama.Framework.Code.SerializableDeclarationId" );

#if ROSLYN_4_8_0
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.DesignTime.4.8.0" );
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.Engine.4.8.0" );
#endif

#if ROSLYN_4_12_0
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.DesignTime.4.12.0" );
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.Engine.4.12.0" );
#endif

#if ROSLYN_5_0_0
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.DesignTime.5.0.0" );
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.Engine.5.0.0" );
#endif
        } );
}