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

[assembly: ExportExtension( typeof( Metalama.Extensions.HtmlWriter.HtmlCodeWriterFactory ), ExtensionKinds.ServiceFactory )]

namespace Metalama.Extensions.HtmlWriter;

/// <summary>
/// Factory that creates <see cref="HtmlCodeWriter"/> instances. This class is used in two ways: through <see cref="ExportExtensionAttribute"/> and <see cref="IProjectServiceFactory"/>
/// (loaded by the pipeline), or as a test plug-in (accessed through <see cref="IHtmlCodeWriterFactory"/>).
/// </summary>
[UsedImplicitly]
public sealed class HtmlCodeWriterFactory : IProjectServiceFactory, IHtmlCodeWriterFactory
{
    IEnumerable<IProjectService> IProjectServiceFactory.CreateServices( in ProjectServiceProvider serviceProvider )
    {
        return [new HtmlCodeWriter( serviceProvider )];
    }

    IHtmlCodeWriter IHtmlCodeWriterFactory.Create( in ProjectServiceProvider serviceProvider ) => new HtmlCodeWriter( serviceProvider );
}
