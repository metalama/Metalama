// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Flashtrace.Formatters.Implementations;

// TODO: Review current impact of #27182 here (especially wrt supported platforms), reinstate compiled regex if possible.

/// <summary>
/// A formatter for <see cref="MethodBase"/> values.
/// </summary>
internal sealed class MethodInfoFormatter : Formatter<MethodBase>
{
#if COMPILED_REGEX
    private const RegexOptions _regexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;
#else
    private const RegexOptions _regexOptions = RegexOptions.CultureInvariant;
#endif

    // (See #26919) We do not create a static Regex instance because it may result in the incorrect AppContext switch values when invoked via module initializer.
    // TODO: [Pre-FT] revert back to the static Regex instance when #27182 is fixed.
    private const string _anonymousMethodRegexPattern = "^(<(?<parent>[^>]+)>b__[0-9A-Fa-f]+|_Lambda)";

    // private static readonly Regex anonymousMethodRegex = new Regex("^(<(?<parent>[^>]+)>b__[0-9A-Fa-f]+|_Lambda)", regexOptions);
    // private static readonly Regex localFunctionRegex = new Regex("<[^>]+>g__[0-9A-Fa-f]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ConcurrentDictionary<MethodBase, Match> _matchCache = new();
    private IFormatter<Type>? _typeFormatter;

    private IFormatter<Type> TypeFormatter
    {
        get
        {
            this._typeFormatter ??= this.Repository.Get<Type>();

            return this._typeFormatter;
        }
    }

    public MethodInfoFormatter( IFormatterRepository repository ) : base( repository ) { }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, MethodBase? value )
    {
        if ( value == null )
        {
            stringBuilder.Append( 'n', 'u', 'l', 'l' );

            return;
        }

        try
        {
            var methodName = value is ConstructorInfo
                ? value.IsStatic ? "StaticConstructor" : "new"
                : value.Name;

            var c = methodName[0];

            if ( c is '<' or '_' )
            {
                if ( !this._matchCache.TryGetValue( value, out var anonymousMethodMatch ) )
                {
                    anonymousMethodMatch = Regex.Match( methodName, _anonymousMethodRegexPattern, _regexOptions );
                    this._matchCache[value] = anonymousMethodMatch;
                }

                if ( anonymousMethodMatch.Success )
                {
                    if ( value.DeclaringType != null )
                    {
                        if ( value.DeclaringType.Name[0] == '<' )
                        {
                            // Don't write the name of the closure class, if any.
                            this.TypeFormatter.Format( stringBuilder, value.DeclaringType.DeclaringType );
                        }
                        else
                        {
                            this.TypeFormatter.Format( stringBuilder, value.DeclaringType );
                        }

                        stringBuilder.Append( '.' );
                    }

                    var parentGroup = anonymousMethodMatch.Groups["parent"];

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    if ( parentGroup != null )
                    {
                        stringBuilder.Append( parentGroup.Value );
                        stringBuilder.Append( ".Anonymous" );
                    }
                    else
                    {
                        stringBuilder.Append( "Anonymous" );
                    }

                    return;
                }
            }

            this.TypeFormatter.Format( stringBuilder, value.DeclaringType );
            stringBuilder.Append( '.' );

            stringBuilder.Append( methodName );

            if ( value.IsGenericMethod )
            {
                var genericArguments = value.GetGenericArguments();
                stringBuilder.Append( '<' );

                for ( var i = 0; i < genericArguments.Length; i++ )
                {
                    if ( i > 0 )
                    {
                        stringBuilder.Append( ',' );
                    }

                    this.TypeFormatter.Format( stringBuilder, genericArguments[i] );
                }

                stringBuilder.Append( '>' );
            }

            stringBuilder.Append( '(' );
            var parameters = value.GetParameters();

            for ( var i = 0; i < parameters.Length; i++ )
            {
                if ( i > 0 )
                {
                    stringBuilder.Append( ',' );
                }

                this.TypeFormatter.Format( stringBuilder, parameters[i].ParameterType );
            }

            stringBuilder.Append( ')' );
        }
        catch ( Exception )
        {
            stringBuilder.Append( value.ToString() );
        }
    }
}