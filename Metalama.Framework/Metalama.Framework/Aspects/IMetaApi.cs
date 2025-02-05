// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Diagnostics;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Exposes information about the declaration to which a template was applied.
    /// This interface is exposed by the <see cref="meta"/> static type.
    /// </summary>
    [CompileTime]
    [InternalImplement]
    internal interface IMetaApi : ISyntaxBuilderImpl
    {
        /// <summary>
        /// Gets access to the declaration being overridden or introduced.
        /// </summary>
        IMetaTarget Target { get; }

        IAspectInstance AspectInstance { get; }

        /// <summary>
        /// Gets an object that gives <c>dynamic</c> access to the instance members of the type. Equivalent to the <c>this</c> C# keyword.
        /// </summary>
        /// <seealso cref="Base"/>
        object This { get; }

        /// <summary>
        /// Gets an object that gives <c>dynamic</c> access to the instance members of the type in the state they were before the application
        /// of the current advice. Equivalent to the <c>base</c> C# keyword.
        /// </summary>
        /// <seealso cref="This"/>
        object Base { get; }

        object ThisType { get; }

        object BaseType { get; }

        IObjectReader Tags { get; }

        ScopedDiagnosticSink Diagnostics { get; }

        void DebugBreak();
    }
}