// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// A <see cref="Formatter{T}"/> for <see cref="Type"/> values.
/// </summary>
internal sealed class TypeFormatter : Formatter<Type>
{
    private readonly bool _includeNamespace;

    private static readonly Dictionary<Type, string> _specialTypeNames =
        new()
        {
            { typeof(string), "string" },
            { typeof(int), "int" },
            { typeof(bool), "bool" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(char), "char" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(float), "float" },
            { typeof(double), "double" }
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeFormatter"/> class specifying whether it should include namespaces.
    /// </summary>
    public TypeFormatter( IFormatterRepository repository, bool includeNamespace = true )
        : base( repository )
    {
        this._includeNamespace = includeNamespace;
    }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, Type? value )
    {
        this.WriteCore( stringBuilder, value );
    }

    private void WriteCore( UnsafeStringBuilder stringBuilder, Type? type, bool removeArity = false, Type[]? genericArguments = null )
    {
        if ( type == null )
        {
            stringBuilder.Append( 'n', 'u', 'l', 'l' );

            return;
        }

        try
        {
            if ( type.IsGenericParameter )
            {
                stringBuilder.Append( type.Name );

                return;
            }

            if ( type.DeclaringType != null )
            {
                // For generic inner type instance, pass the fully specified types on (for A<int>.B<int> argument array contains {int, int}).
                genericArguments ??= type is { IsGenericType: true, IsGenericTypeDefinition: false } ? type.GetGenericArguments() : null;
                this.WriteCore( stringBuilder, type.DeclaringType, genericArguments: genericArguments );
                stringBuilder.Append( '.' );
            }

            if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) )
            {
                this.Format( stringBuilder, type.GetGenericArguments()[0] );
                stringBuilder.Append( '?' );
            }
            else if ( type.IsArray )
            {
                this.Format( stringBuilder, type.GetElementType() );
                stringBuilder.Append( '[' );

                for ( var i = 1; i < type.GetArrayRank(); i++ )
                {
                    stringBuilder.Append( ',' );
                }

                stringBuilder.Append( ']' );
            }
            else if ( type.IsGenericType && (!type.IsGenericTypeDefinition || genericArguments != null) )
            {
                // If generic arguments are set, formatting was started on type instance and the passed array should be used as it contains actual arguments.
                // Otherwise formatting runs on type definition, where we use the local array.
                genericArguments ??= type.GetGenericArguments();
                var genericParameters = type.GetGenericArguments();
                var appendComma = false;

                stringBuilder.Append( RemoveArity( this.GetTypeName( type ), true ) );
                stringBuilder.Append( '<' );

                var startIndex = type.DeclaringType?.GetGenericArguments().Length ?? 0;

                for ( var i = startIndex; i < genericParameters.Length; i++ )
                {
                    if ( appendComma )
                    {
                        stringBuilder.Append( ',' );
                    }
                    else
                    {
                        appendComma = true;
                    }

                    this.Format( stringBuilder, genericArguments[i] );
                }

                stringBuilder.Append( '>' );
            }
            else if ( type.IsGenericTypeDefinition )
            {
                stringBuilder.Append( RemoveArity( this.GetTypeName( type ), true ) );

                if ( !removeArity )
                {
                    stringBuilder.Append( '<' );

                    // Only include number of parameters declared on this type (array contains parameters of declaring types).
                    var startIndex = (type.DeclaringType?.GetGenericArguments().Length ?? 0) + 1;
                    var length = type.GetGenericArguments().Length;

                    for ( var i = startIndex; i < length; i++ )
                    {
                        stringBuilder.Append( ',' );
                    }

                    stringBuilder.Append( '>' );
                }
            }
            else if ( type.IsByRef )
            {
                this.WriteCore( stringBuilder, type.GetElementType(), removeArity );
                stringBuilder.Append( "&" );
            }
            else if ( type.IsPointer )
            {
                this.WriteCore( stringBuilder, type.GetElementType(), removeArity );
                stringBuilder.Append( "*" );
            }
            else if ( !type.HasElementType )
            {
                stringBuilder.Append( RemoveArity( this.GetTypeName( type ), removeArity ) );
            }
            else
            {
                stringBuilder.Append( type.ToString() );
            }
        }
        catch ( Exception )
        {
            // Decent handling of exception in case we have a bug.
            stringBuilder.Append( "!!" );
            stringBuilder.Append( type.ToString() );
        }
    }

    private string GetTypeName( Type type )
    {
        if ( _specialTypeNames.TryGetValue( type, out var typeName ) )
        {
            return typeName;
        }

        if ( type.DeclaringType != null )
        {
            return type.Name;
        }

        return this.IsTrivialNamespace( type )
            ? type.Name
            : type.FullName ?? type.Name;
    }

    private bool IsTrivialNamespace( Type type )
    {
        if ( this._includeNamespace )
        {
            switch ( type.Namespace )
            {
                case "System":
                case "System.Text":
                case "System.Collections":
                case "System.Collections.Generic":
                    return true;

                default:
                    return false;
            }
        }
        else
        {
            return true;
        }
    }

    private static string RemoveArity( string typeName, bool condition )
    {
        if ( condition )
        {
            var indexOfQuote = typeName.LastIndexOf( '`' );

            if ( indexOfQuote > 0 )
            {
                return typeName.Substring( 0, indexOfQuote );
            }
        }

        return typeName;
    }
}