// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    /// <summary>
    /// Contains the set of types that can be serialized by <see cref="SyntaxSerializationService"/>. This class can be used to
    /// determine the possibility to serialize a type when the template is compiled. It may return false positives.
    /// </summary>
    internal sealed class SerializableTypes
    {
        private readonly ImmutableHashSet<string> _serializableTypes;

        public SerializableTypes( IEnumerable<ITypeSymbol> serializableTypes )
        {
            this._serializableTypes = serializableTypes
                .SelectRecursiveInternal( t => t.BaseType )
                .Where( t => t.SpecialType != SpecialType.System_Object )
                .Select( t => t.GetDocumentationCommentId().AssertNotNull() )
                .ToImmutableHashSet();
        }

        private static bool IsSerializableIntrinsic( ITypeSymbol type )
        {
            switch ( type.SpecialType )
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    return true;

                default:
                    return false;
            }
        }

        public bool IsSerializable( ITypeSymbol type, Location? diagnosticLocation = null, IDiagnosticAdder? diagnosticAdder = null )
        {
            if ( IsSerializableIntrinsic( type ) )
            {
                return true;
            }
            else if ( type is IArrayTypeSymbol arrayType )
            {
                return this.IsSerializable( arrayType.ElementType, diagnosticLocation, diagnosticAdder );
            }

            var id = type.GetDocumentationCommentId();

            if ( id == null )
            {
                // This happens for instance for pointer types.
                return false;
            }

            if ( this._serializableTypes.Contains( id ) )
            {
                return true;
            }
            else if ( type is INamedTypeSymbol namedType )
            {
                if ( namedType.EnumUnderlyingType != null )
                {
                    return true;
                }
                else if ( namedType.IsGenericType && !SymbolEqualityComparer.Default.Equals( namedType, namedType.ConstructedFrom ) )
                {
                    if ( namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T )
                    {
                        return this.IsSerializable( namedType.TypeArguments[0], diagnosticLocation, diagnosticAdder );
                    }
                    else if ( this.IsSerializable( namedType.ConstructedFrom, diagnosticLocation, diagnosticAdder ) )
                    {
                        // The child call will report the diagnostic.
                        return namedType.TypeArguments.All( arg => this.IsSerializable( arg, diagnosticLocation, diagnosticAdder ) );
                    }
                }

                if ( namedType.AllInterfaces.Any( i => this.IsSerializable( i ) ) )
                {
                    return true;
                }
            }

            diagnosticAdder?.Report( SerializationDiagnosticDescriptors.UnsupportedSerialization.CreateRoslynDiagnostic( diagnosticLocation, type ) );

            return false;
        }
    }
}