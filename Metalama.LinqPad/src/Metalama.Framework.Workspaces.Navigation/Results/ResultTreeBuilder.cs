// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using Metalama.Framework.Workspaces.Navigation.Facade;
using Metalama.Framework.Workspaces.Navigation.Links;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Workspaces.Navigation.Results;

/// <summary>
/// Summarizes a value as a tree of <see cref="ResultNode"/>, following the display conventions of the facade walker and
/// the limits of <see cref="ResultTreeOptions"/>. Every getter and enumeration runs here, so that the tree no longer
/// depends on the value once it is built.
/// </summary>
internal sealed class ResultTreeBuilder
{
    private readonly ResultTreeOptions _options;
    private readonly LinkBuilder? _linkBuilder;
    private readonly FacadeTypeFactory _facadeTypeFactory;

    public ResultTreeBuilder( ResultTreeOptions options, LinkBuilder? linkBuilder = null, FacadeTypeFactory? facadeTypeFactory = null )
    {
        this._options = options;
        this._linkBuilder = linkBuilder;
        this._facadeTypeFactory = facadeTypeFactory ?? FacadeTypeFactory.Default;
    }

    /// <summary>
    /// Builds the tree of a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="link">The link that resolved the value, when it was reached by a link, so that its children get links too.</param>
    public ResultNode Build( object? value, Link? link = null ) => this.Build( value, link, 0, true );

    private ResultNode Build( object? value, Link? pathLink, int depth, bool expand )
    {
        var link = this._linkBuilder?.GetAnchorLink( value ) ?? pathLink;

        switch ( value )
        {
            case null:
                return new ScalarNode( ScalarKind.Null, null );

            case string s:
                return new ScalarNode( ScalarKind.String, s );

            case bool b:
                return new ScalarNode( ScalarKind.Boolean, b ? "true" : "false" );

            case char c:
                return new ScalarNode( ScalarKind.String, c.ToString() );

            case Severity severity:
                return new ScalarNode( ScalarKind.Severity, severity.ToString() );

            case Enum e:
                return new ScalarNode( ScalarKind.Enum, e.ToString() );

            case FormattableString formattableString:
                return this.BuildRichText( formattableString, depth );

            case IEnumerable enumerable when GetGroupingInterface( value.GetType() ) is { } groupingInterface:
                return this.BuildGrouping( enumerable, groupingInterface, depth, expand );

            case IEnumerable enumerable:
                return this.BuildCollection( enumerable, link, depth, expand );
        }

        var type = value.GetType();

        if ( type.IsPrimitive || value is decimal )
        {
            return new ScalarNode( ScalarKind.Number, Convert.ToString( value, CultureInfo.InvariantCulture ) );
        }

        if ( IsOpaqueValue( value ) )
        {
            return new ScalarNode( ScalarKind.Other, Convert.ToString( value, CultureInfo.InvariantCulture ) );
        }

        return this.BuildObject( value, link, depth, expand );
    }

    private ObjectNode BuildObject( object value, Link? link, int depth, bool expand )
    {
        var declaration = value as IDeclaration;
        ImmutableArray<PropertyNode>? properties = null;

        if ( expand )
        {
            var facadeType = this._facadeTypeFactory.GetFacadeType( value.GetType() );
            var builder = ImmutableArray.CreateBuilder<PropertyNode>( facadeType.Properties.Length );

            foreach ( var property in facadeType.Properties )
            {
                ResultNode propertyValue;

                try
                {
                    propertyValue = this.Build( property.GetFunc( value ), link?.WithStep( property.Name ), depth + 1, depth + 1 < this._options.MaxDepth );
                }
                catch ( Exception e ) when ( e is not OutOfMemoryException )
                {
                    propertyValue = new ErrorNode( e.GetType().Name, e.Message );
                }

                builder.Add( new PropertyNode( property.Name, propertyValue ) );
            }

            properties = builder.MoveToImmutable();
        }

        // Code model objects are internal classes, so they are named by their public interface, such as INamedType.
        var interfaceName = GetInterfaceName( value );

        return new ObjectNode( interfaceName ?? FormatTypeName( value.GetType() ), this.GetDisplay( value ) )
        {
            Link = link,
            Location = declaration != null ? this.GetLocation( declaration ) : null,
            InterfaceName = interfaceName,
            IsDeclaration = declaration != null,
            IsTransformed = link?.IsTransformed ?? false,
            Properties = properties
        };
    }

    private CollectionNode BuildCollection( IEnumerable enumerable, Link? link, int depth, bool expand )
    {
        var accessor = EnumerableAccessor.Get( enumerable.GetType() );
        int? count = accessor.HasCount ? accessor.GetCount( enumerable ) : null;
        var typeName = FormatCollectionTypeName( enumerable.GetType() );

        if ( !expand )
        {
            return new CollectionNode( typeName, count ) { Link = link };
        }

        var items = ImmutableArray.CreateBuilder<ResultNode>();
        var isTruncated = false;
        var index = 0;

        try
        {
            foreach ( var item in enumerable )
            {
                if ( index == this._options.MaxItems )
                {
                    isTruncated = true;

                    break;
                }

                // A collection is shown as rows: an item that is not a declaration, such as a projection to an anonymous
                // type, is expanded to its properties, which become columns. A declaration is expanded only when the depth allows.
                var expandItem = item is IDeclaration ? depth + 1 < this._options.MaxDepth : depth < this._options.MaxDepth;

                items.Add( this.Build( item, link?.WithStep( index.ToString( CultureInfo.InvariantCulture ) ), depth + 1, expandItem ) );
                index++;
            }
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            items.Add( new ErrorNode( e.GetType().Name, e.Message ) );
        }

        if ( count == null && !isTruncated )
        {
            count = index;
        }

        return new CollectionNode( typeName, count ) { Link = link, Items = items.ToImmutable(), IsTruncated = isTruncated || (count != null && count > index) };
    }

    private GroupingNode BuildGrouping( IEnumerable grouping, Type groupingInterface, int depth, bool expand )
    {
        var key = groupingInterface.GetProperty( "Key" )!.GetValue( grouping );
        var items = this.BuildCollection( grouping, null, depth, expand );

        return new GroupingNode( this.Build( key, null, depth + 1, false ), items );
    }

    private RichTextNode BuildRichText( FormattableString formattableString, int depth )
    {
        var formatter = new RichTextFormatter();
        string codedText;

        try
        {
            codedText = formattableString.ToString( formatter );
        }
        catch ( FormatException )
        {
            return new RichTextNode( formattableString.ToString( CultureInfo.InvariantCulture ), [] );
        }

        var segments = ImmutableArray.CreateBuilder<RichTextSegment>();

        foreach ( var token in codedText.Split( RichTextFormatter.Separator ) )
        {
            if ( token.StartsWith( RichTextFormatter.ArgumentPrefix, StringComparison.Ordinal )
                 && int.TryParse( token.AsSpan( RichTextFormatter.ArgumentPrefix.Length ), CultureInfo.InvariantCulture, out var argumentIndex ) )
            {
                var argument = formatter.Arguments[argumentIndex];
                segments.Add( new RichTextSegment( argument.Text, this.Build( argument.Value, null, depth + 1, false ) ) );
            }
            else if ( token.Length > 0 )
            {
                segments.Add( new RichTextSegment( token, null ) );
            }
        }

        var text = string.Concat( segments.Select( s => s.Text ) );

        return new RichTextNode( text, segments.ToImmutable() );
    }

    private string GetDisplay( object value )
    {
        try
        {
            switch ( value )
            {
                case IIntrospectionDiagnostic diagnostic:
                    {
                        var location = diagnostic.FilePath != null
                            ? $" {this.MakeRelative( diagnostic.FilePath )}{(diagnostic.Line != null ? ":" + diagnostic.Line.Value.ToString( CultureInfo.InvariantCulture ) : "")}"
                            : "";

                        return $"{diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Id}{location} {diagnostic.Message}";
                    }

                case IDisplayable displayable:
                    return displayable.ToDisplayString( CodeDisplayFormat.ShortDiagnosticMessage );
            }

            var text = value.ToString();

            return string.IsNullOrWhiteSpace( text ) || text == value.GetType().ToString() ? FormatTypeName( value.GetType() ) : text;
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            return $"‹error: {e.GetType().Name}: {e.Message}›";
        }
    }

    private string? GetLocation( IDeclaration declaration )
    {
        try
        {
            var sources = declaration.Sources;

            if ( sources.IsDefaultOrEmpty )
            {
                return null;
            }

            var span = sources[0].Span;

            if ( string.IsNullOrEmpty( span.FilePath ) )
            {
                return null;
            }

            return $"{this.MakeRelative( span.FilePath )}:{(span.StartLine + 1).ToString( CultureInfo.InvariantCulture )}";
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            return null;
        }
    }

    private string MakeRelative( string path )
    {
        if ( this._options.RootDirectory != null && Path.IsPathFullyQualified( path ) )
        {
            var relative = Path.GetRelativePath( this._options.RootDirectory, path );

            if ( !relative.StartsWith( "..", StringComparison.Ordinal ) )
            {
                path = relative;
            }
        }

        return path.Replace( '\\', '/' );
    }

    /// <summary>
    /// Gets a value indicating whether a value is shown with its string form instead of its properties, such as a
    /// <see cref="DateTime"/>, a <see cref="Guid"/> or a <see cref="Type"/>.
    /// </summary>
    private static bool IsOpaqueValue( object value )
    {
        var type = value.GetType();

        return value is Type or Assembly or MemberInfo or Version or Uri
               || (type.IsValueType && type.Namespace == "System" && !type.IsGenericType && !type.Name.StartsWith( "ValueTuple", StringComparison.Ordinal ));
    }

    private static Type? GetGroupingInterface( Type type )
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>)
            ? type
            : type.GetInterfaces().FirstOrDefault( i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IGrouping<,>) );

    private static string? GetInterfaceName( object value )
        => value switch
        {
            INamedType => nameof(INamedType),
            IConstructor => nameof(IConstructor),
            IMethod => nameof(IMethod),
            IIndexer => nameof(IIndexer),
            IProperty => nameof(IProperty),
            IField => nameof(IField),
            IEvent => nameof(IEvent),
            IParameter => nameof(IParameter),
            ITypeParameter => nameof(ITypeParameter),
            INamespace => nameof(INamespace),
            ICompilation => nameof(ICompilation),
            IAssembly => nameof(IAssembly),
            IDeclaration => nameof(IDeclaration),
            IType => nameof(IType),
            _ => null
        };

    /// <summary>
    /// Formats a type name as C# would show it, without namespaces: <c>ImmutableArray&lt;IMethod&gt;</c>, or
    /// <c>{ Name, Complexity }</c> for an anonymous type.
    /// </summary>
    internal static string FormatTypeName( Type type )
    {
        if ( FacadeType.IsAnonymousType( type ) )
        {
            return "{ " + string.Join( ", ", type.GetProperties().Select( p => p.Name ) ) + " }";
        }

        if ( type.IsArray )
        {
            return FormatTypeName( type.GetElementType()! ) + "[]";
        }

        if ( Nullable.GetUnderlyingType( type ) is { } underlyingType )
        {
            return FormatTypeName( underlyingType ) + "?";
        }

        if ( !type.IsGenericType )
        {
            return type.Name;
        }

        var name = type.Name;
        var backtick = name.IndexOf( '`', StringComparison.Ordinal );

        if ( backtick >= 0 )
        {
            name = name.Substring( 0, backtick );
        }

        return $"{name}<{string.Join( ", ", type.GetGenericArguments().Select( FormatTypeName ) )}>";
    }

    /// <summary>
    /// Formats the type of a collection by its item type, as <c>IEnumerable&lt;IMethod&gt;</c>, unless the collection type is public.
    /// </summary>
    private static string FormatCollectionTypeName( Type type )
    {
        if ( type.IsPublic || type.IsNestedPublic )
        {
            return FormatTypeName( type );
        }

        var enumerableInterface = type.GetInterfaces().FirstOrDefault( i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>) );

        return enumerableInterface != null ? FormatTypeName( enumerableInterface ) : "IEnumerable";
    }

    /// <summary>
    /// Formats the arguments of a <see cref="FormattableString"/> with <see cref="MetalamaStringFormatter"/> and replaces the
    /// arguments that are objects by placeholders, so that they can become nodes.
    /// </summary>
    private sealed class RichTextFormatter : ICustomFormatter, IFormatProvider
    {
        public const string Separator = "\u0001";
        public const string ArgumentPrefix = "\u0002";

        public List<(string Text, object Value)> Arguments { get; } = [];

        public string Format( string? format, object? arg, IFormatProvider? formatProvider )
        {
            var text = FormatArgument( format, arg );

            if ( arg == null || arg is string || arg.GetType().IsPrimitive || arg is Enum )
            {
                return text;
            }

            var index = this.Arguments.Count;
            this.Arguments.Add( (text, arg) );

            return $"{Separator}{ArgumentPrefix}{index.ToString( CultureInfo.InvariantCulture )}{Separator}";
        }

        public object? GetFormat( Type? formatType ) => formatType == typeof(ICustomFormatter) ? this : null;

        private static string FormatArgument( string? format, object? arg )
        {
            try
            {
                return MetalamaStringFormatter.Instance.Format( format, arg, CultureInfo.InvariantCulture );
            }
            catch ( InvalidOperationException )
            {
                // The formatter is initialized with the Metalama engine, which may not have run yet.
                return arg is IFormattable formattable ? formattable.ToString( format, CultureInfo.InvariantCulture ) : arg?.ToString() ?? "";
            }
        }
    }
}
