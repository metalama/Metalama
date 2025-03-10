// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

#pragma warning disable SA1623

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Defines the formatting options of <see cref="IDisplayable.ToDisplayString"/>. Only well-known instances of this classes,
    /// exposed as properties, are currently supported.
    /// </summary>
    [CompileTime]
    public sealed class CodeDisplayFormat
    {
        private readonly string _name;

        // Prevents creation of custom instances.
        private CodeDisplayFormat( string name, bool includeParent )
        {
            this._name = name;
            this.IncludeParent = includeParent;
        }

        /// <summary>
        /// Emits fully-qualified code references, including namespaces and aliases.
        /// </summary>
        public static CodeDisplayFormat FullyQualified { get; } = new( nameof(FullyQualified), true );

        /// <summary>
        /// Formats code references as in a C# error message.
        /// </summary>
        public static CodeDisplayFormat DiagnosticMessage { get; } = new( nameof(DiagnosticMessage), true );

        /// <summary>
        /// Emits minimally-qualified code references.
        /// </summary>
        public static CodeDisplayFormat MinimallyQualified { get; } = new( nameof(MinimallyQualified), false );

        /// <summary>
        /// Formats code references as in a C# short error message.
        /// </summary>
        public static CodeDisplayFormat ShortDiagnosticMessage { get; } = new( nameof(ShortDiagnosticMessage), true );

        internal bool IncludeParent { get; }

        public override string ToString() => this._name;
    }
}