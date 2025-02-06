// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.Collections.Immutable;
using static Metalama.Patterns.Wpf.Implementation.StringHelper;

namespace Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;

[CompileTime]
internal sealed class DefaultCommandNamingConvention : ICommandNamingConvention
{
    public static string RegistrationKey { get; } = "default";

    public string Name => "default";

    public CommandNamingConventionMatch Match( IMethod executeMethod )
    {
        var commandName = GetCommandNameFromExecuteMethodName( executeMethod.Name );

        var commandPropertyName = GetCommandPropertyNameFromCommandName( commandName );

        var canExecuteName = GetCanExecuteNameFromCommandName( commandName );

        return CommandNamingConventionMatcher.Match(
            this,
            executeMethod,
            commandPropertyName,
            new StringNameMatchPredicate( canExecuteName ) );
    }

    public static string GetCommandNameFromExecuteMethodName( string name )
    {
        var nameWithoutTrivialPrefix = name;
        TrimStart( ref nameWithoutTrivialPrefix, "_", StringComparison.OrdinalIgnoreCase );
        TrimStart( ref nameWithoutTrivialPrefix, "m_", StringComparison.OrdinalIgnoreCase );

        var useName = nameWithoutTrivialPrefix;

        TrimStart( ref useName, "execute", StringComparison.OrdinalIgnoreCase );
        TrimStart( ref useName, "_", StringComparison.OrdinalIgnoreCase );
        
        TrimEnd( ref useName, "async", StringComparison.OrdinalIgnoreCase );
        
        _ = TrimEnd( ref useName, "_command", StringComparison.OrdinalIgnoreCase )
            || TrimEnd( ref useName, "command", StringComparison.OrdinalIgnoreCase );

        if ( string.IsNullOrEmpty( useName ) )
        {
            // It's an unusual name comprised of expected prefixes and/or suffixes.
            // Just use it as-is.

            if ( !string.IsNullOrEmpty( nameWithoutTrivialPrefix ) )
            {
                return nameWithoutTrivialPrefix;
            }
            else
            {
                return name;
            }
        }

        if ( char.IsLower( useName[0] ) )
        {
            useName = char.ToUpperInvariant( useName[0] ) + useName.Substring( 1 );
        }

        return useName;
    }

    public static string GetCommandPropertyNameFromCommandName( string commandName ) => $"{commandName}Command";

    public static ImmutableArray<string> GetCanExecuteNameFromCommandName( string commandName )
        => ImmutableArray.Create( $"CanExecute{commandName}", $"Can{commandName}", $"Is{commandName}Enabled" );
}