// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;
using Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.Text.RegularExpressions;

namespace Metalama.Patterns.Wpf.Configuration;

// Prevent netframework-only false positives

#if NETFRAMEWORK
#pragma warning disable CS8604 // Possible null reference argument.
#endif

#pragma warning disable SA1623

[CompileTime]
[PublicAPI]
public sealed record CommandNamingConvention : ICommandNamingConvention
{
    private const string _commandNameGroup = "CommandName";
    private const string _commandNameToken = "{CommandName}";

    /// <summary>
    /// Gets or sets the name of the naming convention. 
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets a regular expression pattern that will be evaluated against the name of the target method of the <see cref="CommandAttribute"/> aspect. 
    /// The expression should yield a match group named <c>CommandName</c>. If <see cref="CommandNamePattern"/> is not specified,
    /// the name produced from the target method name, minus any <c>Execute</c> prefix and <c>Command</c> suffix. 
    /// </summary>
    public string? CommandNamePattern { get; init; }

    /// <summary>
    /// Gets or sets the name of the command property to be introduced. The substring  <c>{CommandName}</c> will be replaced with the
    /// command name determined according to <see cref="CommandNamePattern"/>. If <see cref="CommandPropertyName"/> is not specified
    /// the name produced by appending the <c>Command</c> suffix to the command name.
    /// </summary>
    public string? CommandPropertyName { get; init; }

    /// <summary>
    /// Gets or sets a list of regular expression patterns that will be evaluated against method and/or property names to identify candidate can-execute members. 
    /// In this pattern, all occurrences of the substring <c>{CommandName}</c> will be replaced with the command name
    /// determined according to <see cref="CommandNamePattern"/> before the expression is evaluated.
    /// </summary>
    public string[]? CanExecutePatterns { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether a matching valid unambiguous can-execute method or property must be found for a match to be considered successful.
    /// The default value is <see langword="true"/> when <see cref="CanExecutePatterns"/> is specified, otherwise <see langword="false"/>.
    /// </summary>
    public bool? IsCanExecuteRequired { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the aspect should look for a <c>CanExecute</c> <i>method</i>. The default value is <c>true</c>.
    /// </summary>
    public bool ConsiderCanExecuteMethod { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the aspect should look for a <c>CanExecute</c> <i>property</i>. The default value is <c>true</c>.
    /// </summary>
    public bool ConsiderCanExecuteProperty { get; init; } = true;

    [NonCompileTimeSerialized]
    private Regex? _commandNameRegex;

    public CommandNamingConvention( string name )
    {
        this.Name = name;
    }

    CommandNamingConventionMatch INamingConvention<IMethod, CommandNamingConventionMatch>.Match( IMethod executeMethod )
    {
        string? commandName = null;

        if ( this.CommandNamePattern != null )
        {
            this._commandNameRegex ??= new Regex( this.CommandNamePattern );

            var m = this._commandNameRegex.Match( executeMethod.Name );

            if ( m.Success )
            {
                var g = m.Groups[_commandNameGroup];

                if ( g.Success )
                {
                    commandName = g.Value;
                }
            }
        }
        else
        {
            commandName = DefaultCommandNamingConvention.GetCommandNameFromExecuteMethodName( executeMethod.Name );
        }

        if ( string.IsNullOrWhiteSpace( commandName ) )
        {
            return new CommandNamingConventionMatch(
                this,
                null,
                MemberMatch<IMemberOrNamedType, DefaultMatchKind>.Invalid(),
                MemberMatch<IMember, DefaultMatchKind>.NotFound(),
                [],
                this.IsCanExecuteRequired.GetValueOrDefault( this.CanExecutePatterns != null ) );
        }

#if NETCOREAPP
#pragma warning disable CA1307 // Specify StringComparison for clarity
#endif
        var commandPropertyName = this.CommandPropertyName != null
            ? this.CommandPropertyName.Replace( _commandNameToken, commandName )
            : DefaultCommandNamingConvention.GetCommandPropertyNameFromCommandName( commandName );

        INameMatchPredicate canExecutePredicate;

        if ( this.CanExecutePatterns != null )
        {
            var pattern = string.Join( "|", this.CanExecutePatterns.Select( x => x.Replace( _commandNameToken, commandName ) ) );

            canExecutePredicate =
                new RegexNameMatchPredicate( new Regex( $"^{pattern}$" ) );
        }
        else
        {
            canExecutePredicate = new StringNameMatchPredicate( DefaultCommandNamingConvention.GetCanExecuteNameFromCommandName( commandName ) );
        }

#if NETCOREAPP
#pragma warning restore CA1307 // Specify StringComparison for clarity
#endif

        return CommandNamingConventionMatcher.Match(
            this,
            executeMethod,
            commandPropertyName,
            canExecutePredicate,
            considerMethod: this.ConsiderCanExecuteMethod,
            considerProperty: this.ConsiderCanExecuteProperty,
            requireCanExecuteMatch: this.IsCanExecuteRequired.GetValueOrDefault( this.CanExecutePatterns != null ) );
    }
}