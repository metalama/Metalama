// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Remoting;

public sealed class SerializationBinderTests
{
    private readonly JsonSerializationBinder _binder = new JsonSerializationBinderProvider().Binder;

    [Theory]
    [InlineData( typeof(int) )]
    [InlineData( typeof(ProjectKey) )]
    [InlineData( typeof(ImmutableArray<string>) )]
    [InlineData( typeof(ImmutableArray<ProjectKey>) )]

    // Every contract DTO type must be on the #1651 allow-list. These are registered by full-name string in
    // JsonSerializationBinderProvider (not via typeof, to avoid the multi-version eager-load crash of #31075), so this
    // theory guards against a typo in those strings: a mismatch makes BindToType reject the type and fails the test.
    [InlineData( typeof(Metalama.Framework.DesignTime.AspectExplorer.AspectDatabaseAspectInstance) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.AspectExplorer.AspectDatabaseAspectTransformation) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.VisualStudio.AspectExplorer.AspectClassesChangedEventData) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.VisualStudio.AspectExplorer.AspectInstancesChangedEventData) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.CodeLens.CodeLensSummary) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.CodeLens.CodeLensDetailsTable) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.CodeLens.CodeLensDetailsHeader) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.CodeLens.CodeLensDetailsEntry) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.CodeLens.CodeLensDetailsField) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.Diagnostics.DiagnosticData) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus.CompileTimeEditingStatusChangedEventData) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus.CompileTimeErrorsChangedEventData) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.VisualStudio.ServiceProvider.RpcServiceInfo) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.VisualStudio.ServiceProvider.ServicesAddedEventData) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.VisualStudio.SourceGenerating.GeneratedSourceChangedEventData) )]
    [InlineData( typeof(Metalama.Framework.DesignTime.Preview.SerializablePreviewTransformationResult) )]
    [InlineData( typeof(Metalama.Framework.Engine.DesignTime.SerializableSyntaxTree) )]
    [InlineData( typeof(Metalama.Framework.Engine.DesignTime.SerializableAnnotation) )]
    [InlineData( typeof(Metalama.Framework.Code.SerializableDeclarationId) )]
    public void Binder( Type type )
    {
        this._binder.BindToName( type, out var assemblyName, out var typeName );
        assemblyName = JsonSerializationBinderHelper.RemoveAssemblyDetailsFromAssemblyName( assemblyName! );
        typeName = JsonSerializationBinderHelper.RemoveAssemblyDetailsFromTypeName( typeName! );
        var roundloopType = this._binder.BindToType( assemblyName, typeName );
        Assert.Same( type, roundloopType );
    }

    [Theory]
    [InlineData(
        typeof(ImmutableArray<ProjectKey>),
        "System.Collections.Immutable.ImmutableArray`1[[Metalama.Framework.DesignTime.Rpc.ProjectKey, Metalama.Framework.DesignTime.Rpc, VERSION]]" )]
    [InlineData(
        typeof(ImmutableDictionary<string, string>),
        "System.Collections.Immutable.ImmutableDictionary`2[[System.String, System.Private.CoreLib, VERSION],[System.String, System.Private.CoreLib, VERSION]]" )]
    public void QualifyTypeName( Type type, string expectedQualifiedName )
    {
        this._binder.BindToName( type, out _, out var typeName );
        typeName = JsonSerializationBinderHelper.RemoveAssemblyDetailsFromTypeName( typeName! );

        var qualified = JsonSerializationBinderHelper.QualifyAssemblies(
            typeName,
            new Dictionary<string, string>()
            {
                { "Metalama.Framework.DesignTime.Rpc", "Metalama.Framework.DesignTime.Rpc, VERSION" },
                { "System.Collections.Immutable", "System.Collections.Immutable, VERSION" },
                { "System.Private.CoreLib", "System.Private.CoreLib, VERSION" },
                { "mscorlib", "System.Private.CoreLib, VERSION" }
            } );

        Assert.Equal( expectedQualifiedName, qualified );
    }
}