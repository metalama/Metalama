// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System.Linq;

namespace Metalama.Testing.AspectTesting;

internal static class TestContextExtensions
{
    extension( TestContext testContext )
    {
        public IDiffToolRunner? DiffToolRunner => testContext.PlugIns.OfType<IDiffToolRunner>().SingleOrDefault();

        public IHtmlCodeWriter? CreateHtmlCodeWriter( in ProjectServiceProvider serviceProvider )
            => testContext.PlugIns.OfType<IHtmlCodeWriterFactory>().SingleOrDefault()?.Create( serviceProvider );
    }
}