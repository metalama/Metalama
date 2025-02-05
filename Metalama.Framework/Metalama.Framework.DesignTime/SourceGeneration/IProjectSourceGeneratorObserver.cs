// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.SourceGeneration;

internal interface IProjectSourceGeneratorObserver : IGlobalService
{
    void OnGeneratedCodePublished( ProjectKey projectKey, ImmutableDictionary<string, string> sources );

    void OnTouchFileWritten( ProjectKey projectKey, string content );
}