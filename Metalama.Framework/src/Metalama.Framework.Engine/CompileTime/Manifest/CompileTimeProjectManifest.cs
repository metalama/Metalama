// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine.Serialization;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Manifest
{
    /// <summary>
    /// A serializable object that stores the manifest of a <see cref="CompileTimeProject"/>.
    /// </summary>
    internal sealed class CompileTimeProjectManifest
    {
        public CompileTimeProjectManifest(
            string runTimeAssemblyIdentity,
            string targetFramework,
            IReadOnlyList<string> aspectTypes,
            IReadOnlyList<string> plugInTypes,
            IReadOnlyList<string> fabricTypes,
            IReadOnlyList<string> transitiveFabricTypes,
            IReadOnlyList<string> otherTemplateTypes,
            IReadOnlyList<string> optionTypes,
            IReadOnlyList<string>? references,
            TemplateProjectManifest? templates,
            ulong sourceHash,
            IReadOnlyList<CompileTimeFileManifest> files,
            IReadOnlyList<CompileTimeDiagnosticManifest> diagnostics,
            bool referencesMetalamaSdk,
            int manifestVersion = 0,
            LanguageVersion? languageVersion = null,
            string? metalamaVersion = null )
        {
            this.RunTimeAssemblyIdentity = runTimeAssemblyIdentity;
            this.TargetFramework = targetFramework;
            this.AspectTypes = aspectTypes;
            this.PlugInTypes = plugInTypes;
            this.FabricTypes = fabricTypes;
            this.TransitiveFabricTypes = transitiveFabricTypes;
            this.OtherTemplateTypes = otherTemplateTypes;
            this.OptionTypes = optionTypes;
            this.References = references;
            this.Templates = templates;
            this.SourceHash = sourceHash;
            this.Files = files;
            this.Diagnostics = diagnostics;
            this.ReferencesMetalamaSdk = referencesMetalamaSdk;
            // Restored from the manifest when there is one, so that a deserialized manifest keeps the version that
            // *wrote* it, and defaulted to the current version only when a manifest is being created. Same pattern as
            // ManifestVersion below. Without this the property silently reported the reading version, which made it
            // useless for telling how a reference was built, and made LAMA0061 name the wrong version.
            this.MetalamaVersion = string.IsNullOrEmpty( metalamaVersion )
                ? AssemblyMetadataReader.GetInstance( typeof(CompileTimeProjectManifest).Assembly ).PackageVersion.AssertNotNull()
                : metalamaVersion!;
            this.ManifestVersion = manifestVersion == 0 ? CurrentManifestVersion : manifestVersion;
            this.LanguageVersion = languageVersion;

#if DEBUG

            // Validate that we got a valid target framework.
            if ( !string.IsNullOrEmpty( targetFramework ) )
            {
                _ = new FrameworkName( targetFramework );
            }
#endif
        }

        // We intentionally don't include the project hash because it differs among Metalama builds.

        public string RunTimeAssemblyIdentity { get; }

        public string TargetFramework { get; }

        /// <summary>
        /// Gets the version of Metalama that created the compile-time project.
        /// </summary>
        /// <remarks>
        /// Round-trips through serialization: a manifest read from a reference reports the version that produced that
        /// reference, not the version reading it.
        /// </remarks>
        public string MetalamaVersion { get; }

        public int ManifestVersion { get; }

        public const int CurrentManifestVersion = 1;

        // We're explicitly serializing as an integer because the manifest might be deserialized by a lower Roslyn
        // version than the one serializing it.
        [JsonConverter( typeof(LanguageVersionJsonConverter) )]
        public LanguageVersion? LanguageVersion { get; set; }

        /// <summary>
        /// Gets the C# language version at which the compile-time code of this project was compiled. It is the version
        /// stored in the manifest, or C# 13 when the manifest carries none, because the versions of Metalama that did
        /// not write the property never compiled compile-time code above C# 13.
        /// </summary>
        /// <remarks>
        /// The stored version is the one that <see cref="Templating.TemplateCompiler.TemplateLanguageVersion"/> used,
        /// that is, the value of the <c>MetalamaTemplateLanguageVersion</c> property when the project sets it, and
        /// otherwise the language version of the project capped by the version that its .NET SDK supports. It is not
        /// derived from the language features that the compile-time code actually uses, so it is an upper bound and
        /// not a requirement.
        /// <para>
        /// This value can be higher than any version that the Roslyn of the current process accepts, because the
        /// manifest may have been written by a higher Roslyn version than the one reading it. Use
        /// <see cref="ResolvedLanguageVersion"/> to parse or to compile, and this property only to report the version
        /// that the project requires.
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public LanguageVersion RequiredLanguageVersion => this.LanguageVersion ?? AllLanguageVersions.CSharp13;

        /// <summary>
        /// Gets the language version at which the compile-time code of this project must be parsed and compiled, which
        /// is <see cref="RequiredLanguageVersion"/> clamped to the highest version that the Roslyn variant of the
        /// current process accepts.
        /// </summary>
        /// <remarks>
        /// Without the clamp, Roslyn reports <c>CS8192</c>, "Provided language version is unsupported or invalid", on
        /// every syntax tree of the compile-time project, and the whole compile-time build of the reference fails. That
        /// error names a number and not the reference that requires it, which is what issue #1185 reported. With the
        /// clamp, the compile-time code is parsed at the highest version available, so the errors name the language
        /// features that this version does not accept. The caller reports the warning. See issue #1928.
        /// </remarks>
        [JsonIgnore]
        public LanguageVersion ResolvedLanguageVersion
        {
            get
            {
                var requiredLanguageVersion = this.RequiredLanguageVersion;
                var maxLanguageVersion = RoslynApiVersion.Current.ToLanguageVersion();

                return requiredLanguageVersion > maxLanguageVersion ? maxLanguageVersion : requiredLanguageVersion;
            }
        }

        /// <summary>
        /// Gets the list of all aspect types (specified by fully qualified name) of the aspect library.
        /// </summary>
        public IReadOnlyList<string> AspectTypes { get; }

        /// <summary>
        /// Gets the list of all template types (specified by fully qualified name) that are neither aspects nor fabrics in the aspect library.
        /// </summary>
        public IReadOnlyList<string> OtherTemplateTypes { get; }

        /// <summary>
        /// Gets the list of types that are exported using the <c>CompilerPlugin</c> attribute.
        /// </summary>
        public IReadOnlyList<string> PlugInTypes { get; }

        /// <summary>
        /// Gets the list of types that implement the <see cref="Metalama.Framework.Fabrics.Fabric"/> interface, but the <see cref="Metalama.Framework.Fabrics.TransitiveProjectFabric"/>.
        /// </summary>
        public IReadOnlyList<string> FabricTypes { get; }

        /// <summary>
        /// Gets the list of types that implement the <see cref="Metalama.Framework.Fabrics.TransitiveProjectFabric"/> interface.
        /// </summary>
        public IReadOnlyList<string> TransitiveFabricTypes { get; }

        /// <summary>
        /// Gets the list of types that implement the <see cref="IHierarchicalOptions"/> interface.
        /// </summary>
        public IReadOnlyList<string> OptionTypes { get; }

        public TemplateProjectManifest? Templates { get; }

        /// <summary>
        /// Gets the name of all project references (a fully-qualified assembly identity) of the compile-time project.
        /// </summary>
        public IReadOnlyList<string>? References { get; }

        /// <summary>
        /// Gets a unique hash of the source code and its dependencies.
        /// </summary>
        public ulong SourceHash { get; }

        /// <summary>
        /// Gets the list of code files.
        /// </summary>
        public IReadOnlyList<CompileTimeFileManifest> Files { get; }

        /// <summary>
        /// Gets the list of diagnostics produced during the compilation.
        /// </summary>
        public IReadOnlyList<CompileTimeDiagnosticManifest>? Diagnostics { get; }

        public bool ReferencesMetalamaSdk { get; }

        public static CompileTimeProjectManifest Deserialize( Stream stream )
        {
            using var manifestReader = new StreamReader( stream, Encoding.UTF8 );
            var manifestJson = manifestReader.ReadToEnd();
            stream.Close();

            var manifest = FromJson( manifestJson );

            // Assert that files are properly deserialized.
            foreach ( var file in manifest.Files )
            {
                if ( file.SourcePath == null! || file.TransformedPath == null! )
                {
                    throw new AssertionFailedException( "Deserialization error." );
                }
            }

            return manifest;
        }

        public static CompileTimeProjectManifest FromJson( string json ) => ManifestSerializer.Deserialize<CompileTimeProjectManifest>( json );

        public void Serialize( Stream stream )
        {
            var manifestJson = this.ToJson();
            using var manifestWriter = new StreamWriter( stream, Encoding.UTF8 );
            manifestWriter.Write( manifestJson );
        }

        public string ToJson() => ManifestSerializer.Serialize( this );
    }
}