// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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