// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable InconsistentNaming

using JetBrains.Annotations;

namespace Metalama.Framework.Engine.Options;

[PublicAPI]
public static class MSBuildItemNames
{
    public const string MetalamaCompileTimePackage = nameof(MetalamaCompileTimePackage);

    public const string MetalamaSourceGeneratorAttribute = nameof(MetalamaSourceGeneratorAttribute);
}