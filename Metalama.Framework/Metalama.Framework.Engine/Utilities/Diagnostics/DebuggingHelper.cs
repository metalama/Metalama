// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using Metalama.Compiler;
using Metalama.Framework.Engine.Pipeline;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Metalama.Framework.Engine.Utilities.Diagnostics
{
    [ExcludeFromCodeCoverage]
    public static class DebuggingHelper
    {
        private static readonly ConditionalWeakTable<object, ObjectId> _objectIds = new();

        public static void RequireMetalamaCompiler()
        {
            if ( ProcessUtilities.ProcessKind == ProcessKind.Compiler && !MetalamaCompilerInfo.IsActive )
            {
                throw new AssertionFailedException( "Metalama is running in the vanilla C# compiler instead of the customized one." );
            }
        }

        private static int GetObjectIdImpl( object? o ) => o == null ? 0 : _objectIds.GetOrCreateValue( o ).Id;

        // The argument type must be specified explicitly to make sure we are not creating ids for unwanted objects.
        // This avoids e.g. confusion between PartialCompilation and Compilation.
        public static int GetObjectId( Compilation o ) => GetObjectIdImpl( o );

        public static int GetObjectId( AspectPipelineConfiguration? o ) => GetObjectIdImpl( o );

        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class ObjectId
        {
            public int Id { get; }

            private static int _nextId;

            public ObjectId()
            {
                this.Id = Interlocked.Increment( ref _nextId );
            }
        }
    }
}