// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using LINQPad;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.LinqPad
{
    /// <summary>
    /// A helper class used by <see cref="FacadeObject"/> to format property values into LinqPad view objects.
    /// </summary>
    internal static class FacadePropertyFormatter
    {
        public static object FormatLazyPropertyValue( object owner, Type propertyType, Func<object, object?> getter )
        {
            if ( getter == null )
            {
                throw new ArgumentNullException( nameof(getter) );
            }

            if ( owner == null )
            {
                throw new ArgumentNullException( nameof(owner) );
            }

            return typeof(FacadePropertyFormatter)
                .GetMethod( nameof(CreateLazy), BindingFlags.Static | BindingFlags.NonPublic )
                .AssertNotNull()
                .MakeGenericMethod( propertyType )
                .Invoke( null, [new Func<object?>( () => getter( owner ) )] )!;
        }

        public static object? FormatPropertyValue( object? value ) => FormatPropertyValueTestable( value ).View;

        public static (object? View, object? ViewModel) FormatPropertyValueTestable( object? value )
        {
            switch ( value )
            {
                case null:
                    return (null, null);

                case Permalink permalink:
                    return (permalink.Format(), null);

                case Severity severity:
                    {
                        var sign = severity switch
                        {
                            Severity.Warning => "\u26A0",
                            Severity.Error => "\u26D4",
                            Severity.Info => "\u2139",
                            Severity.Hidden => "\uD83D\uDEC7",
                            _ => ""
                        };

                        return (sign + " " + severity.ToString(), severity);
                    }
            }

            if ( value.GetType().IsEnum )
            {
                return (value.ToString(), null);
            }

            var groupingInterface = value.GetType().GetInterface( "System.Linq.IGrouping`2" );

            if ( groupingInterface != null )
            {
                value = typeof(FacadePropertyFormatter)
                    .GetMethod( nameof(CreateGrouping), BindingFlags.Static | BindingFlags.NonPublic )
                    .AssertNotNull()
                    .MakeGenericMethod( groupingInterface.GetGenericArguments() )
                    .Invoke( null, [value] )!;
            }

            if ( IsComplexType( value ) )
            {
                return CreateHyperlinkTestable( value );
            }
            else
            {
                return (value, value);
            }
        }

        private static (object? View, object? ViewModel) CreateHyperlinkTestable( object value )
        {
            string? summary;
            string? cssClass = null;

            switch ( value )
            {
                case IEnumerable:
                    cssClass = "collection";

                    if ( EnumerableAccessor.Get( value.GetType() ) is { HasCount: true } accessor )
                    {
                        var count = accessor.GetCount( value );

                        switch ( count )
                        {
                            case 0:
                                // Did not managed to render this with some styling. The following did not work:
                                // Util.RawHtml, Util.WithStyle
                                return ("(empty)", value);

                            case 1:
                                summary = "(1 item)";

                                break;

                            default:
                                summary = $"({count} items)";

                                break;
                        }
                    }
                    else
                    {
                        summary = "(? items)";
                    }

                    break;

                case FormattableString formattableString:
                    return (CreateRichText( formattableString ), null);

                case IDisplayable displayable:
                    summary = displayable.ToDisplayString( CodeDisplayFormat.ShortDiagnosticMessage );

                    break;

                default:
                    summary = value.ToString();

                    if ( string.IsNullOrWhiteSpace( summary ) )
                    {
                        summary = value.GetType().Name;
                    }

                    break;
            }

            var hyperlink = CreateHyperlink( value, summary, cssClass );

            return (hyperlink, value);
        }

        private static bool _alreadyAddedCss;

        public static object FormatException( Exception exception )
        {
            if ( !_alreadyAddedCss )
            {
                _alreadyAddedCss = true;
            }

            return CreateHyperlink( exception, exception.Message, "error" );
        }

        private static Lazy<T> CreateLazy<T>( Func<object?> func ) => new( () => (T) func()! );

        private static GroupingFacade<TKey, TItems> CreateGrouping<TKey, TItems>( IGrouping<TKey, TItems> group ) => new( group );

        private static object CreateHyperlink( object o, string summary, string? cssClass = null )
        {
            DumpContainer container = new();

            Hyperlinq link = new(
                () => container.Content = o, // On click, replace the link by the object content.
                summary );

            if ( cssClass != null )
            {
                link.CssClass = cssClass;
            }

            container.Content = link;

            return container;
        }

        private static object CreateRichText( FormattableString formattableString )
        {
            DumpContainer container = new();
            var verticalElements = new List<object>();
            var horizontalElements = new List<object>();

            var formatter = new RichTextFormatter();

            var codedString = formattableString.ToString( formatter );

            foreach ( var token in codedString.Split( RichTextFormatter.Separator ) )
            {
                if ( token.StartsWith( RichTextFormatter.ArgumentPrefix, StringComparison.OrdinalIgnoreCase ) )
                {
                    var argId = int.Parse( token.Substring( RichTextFormatter.ArgumentPrefix.Length ), CultureInfo.InvariantCulture );
                    var arg = formatter.Arguments[argId];

                    var isExpanded = false;

                    Hyperlinq link = new(
                        () =>
                        {
                            if ( !isExpanded )
                            {
                                verticalElements.Add( arg.Object );
                                container.UpdateContent( Util.HorizontalRun( false, horizontalElements ) );
                                isExpanded = true;
                            }
                        }, // On click, replace the link by the object content.
                        arg.Summary );

                    horizontalElements.Add( link );
                }
                else
                {
                    horizontalElements.Add( token );
                }
            }

            verticalElements.Add( Util.HorizontalRun( false, horizontalElements ) );

            container.Content = Util.VerticalRun( verticalElements );

            return container;
        }

        private static bool IsComplexType( object? obj )
        {
            if ( obj == null )
            {
                return false;
            }

            var type = obj.GetType();

            if ( type.IsPrimitive || type.IsEnum || type == typeof(string) )
            {
                return false;
            }

            return true;
        }

        private sealed class RichTextFormatter : ICustomFormatter, IFormatProvider
        {
            public const string Separator = "|||";
            public const string ArgumentPrefix = "arg:";

            public List<RichTextToken> Arguments { get; } = [];

            public string Format( string? format, object? arg, IFormatProvider? formatProvider )
            {
                if ( arg == null )
                {
                    return "";
                }
                else if ( IsComplexType( arg ) )
                {
                    var tokenId = this.Arguments.Count;
                    this.Arguments.Add( new RichTextToken( MetalamaStringFormatter.Instance.Format( format, arg, formatProvider ), arg ) );

                    return $"{Separator}{ArgumentPrefix}{tokenId}{Separator}";
                }
                else
                {
                    return MetalamaStringFormatter.Instance.Format( format, arg, formatProvider );
                }
            }

            public object GetFormat( Type? formatType ) => this;
        }

        private sealed record RichTextToken( string Summary, object Object );
    }
}