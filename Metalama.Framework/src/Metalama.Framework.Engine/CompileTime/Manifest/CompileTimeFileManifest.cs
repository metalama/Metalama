// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Templating.Mapping;
using Newtonsoft.Json;

namespace Metalama.Framework.Engine.CompileTime.Manifest
{
    /// <summary>
    /// Represents a file in a <see cref="CompileTimeProject"/>. This class is serialized
    /// to Json as a part of the <see cref="CompileTimeProjectManifest"/>.
    /// </summary>
    [JsonObject]
    internal sealed class CompileTimeFileManifest
    {
        // TODO: Add serialization-deserialization tests because this is brittle.

        /// <summary>
        /// Gets the source path.
        /// </summary>
        public string SourcePath { get; init; }

        /// <summary>
        /// Gets the transformed path (relatively to the root of the archive).
        /// </summary>
        public string TransformedPath { get; init; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public CompileTimeFileManifest()
#pragma warning restore CS8618 // Elements should appear in the correct order
        {
            // Deserializer.
        }

        public CompileTimeFileManifest( TextMapFile textMapFile )
        {
            this.SourcePath = textMapFile.SourcePath;
            this.TransformedPath = textMapFile.TargetPath;
        }
    }
}