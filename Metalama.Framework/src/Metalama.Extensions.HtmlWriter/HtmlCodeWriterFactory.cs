// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using System.Collections.Generic;

[assembly: ExportExtension( typeof( Metalama.Extensions.HtmlWriter.HtmlCodeWriterFactory ), ExtensionKinds.ServiceFactory )]

namespace Metalama.Extensions.HtmlWriter;

/// <summary>
/// Factory that creates <see cref="HtmlCodeWriter"/> instances.
/// </summary>
public sealed class HtmlCodeWriterFactory : IProjectServiceFactory
{
    /// <inheritdoc />
    public IEnumerable<IProjectService> CreateServices( in ProjectServiceProvider serviceProvider )
    {
        return [new HtmlCodeWriter( serviceProvider )];
    }
}
