// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Testing.AspectTesting;
using System.Collections.Generic;

[assembly: ExportExtension( typeof( Metalama.Extensions.HtmlWriter.HtmlCodeWriterServiceFactory ), ExtensionKinds.ServiceFactory )]

namespace Metalama.Extensions.HtmlWriter;

/// <summary>
/// Factory that creates <see cref="HtmlCodeWriter"/> instances for the compile-time pipeline.
/// This class is loaded via <see cref="ExportExtensionAttribute"/> and must not have dependencies on test assemblies.
/// </summary>
[UsedImplicitly]
public sealed class HtmlCodeWriterServiceFactory : IProjectServiceFactory
{
    IEnumerable<IProjectService> IProjectServiceFactory.CreateServices( in ProjectServiceProvider serviceProvider )
    {
        return [new HtmlCodeWriter( serviceProvider )];
    }
}

/// <summary>
/// Factory that creates <see cref="HtmlCodeWriter"/> instances for the test framework.
/// This class implements <see cref="IHtmlCodeWriterFactory"/> for use by test plug-ins.
/// </summary>
[UsedImplicitly]
public sealed class HtmlCodeWriterFactory : IHtmlCodeWriterFactory
{
    IHtmlCodeWriter IHtmlCodeWriterFactory.Create( in ProjectServiceProvider serviceProvider ) => new HtmlCodeWriter( serviceProvider );
}
