// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.Implementations;
using Flashtrace.Formatters.TypeExtensions;
using JetBrains.Annotations;
using System.Collections.Concurrent;

namespace Flashtrace.Formatters;

/// <summary>
/// Allows to get and register formatters for a specific type.
/// </summary>
[PublicAPI]
public sealed partial class FormatterRepository : IFormatterRepository
{
    private readonly CovariantTypeExtensionFactory<IFormatter, IFormatterRepository> _formatterFactory;
    private readonly CovariantTypeExtensionFactory<IFormatter, IFormatterRepository> _dynamicFormatterFactory;

    private readonly ConcurrentDictionary<Type, InvariantFormatterCacheEntry> _invariantFormatterCache = new();
    private readonly ConcurrentDictionary<Type, TypeExtensionInfo<IFormatter>> _dynamicFormatterCache = new();

    private readonly Func<Type, TypeExtensionInfo<IFormatter>> _getFormatterFunc;
    private readonly Func<Type, InvariantFormatterCacheEntry> _getInvariantFormatterFunc;

    private FormatterRepository( FormattingRole role, Action<Builder>? build = null )
    {
        this.Role = role ?? throw new ArgumentNullException( nameof(role) );
        var roleType = role.GetType();

        this._formatterFactory =
            new CovariantTypeExtensionFactory<IFormatter, IFormatterRepository>( typeof(IFormatter<>), typeof(FormatterConverter<,>), roleType, this );

        this._dynamicFormatterFactory =
            new CovariantTypeExtensionFactory<IFormatter, IFormatterRepository>( typeof(IFormatter<>), typeof(FormatterConverter<,>), roleType, this );

        this._getFormatterFunc = type =>
            this._dynamicFormatterFactory.GetTypeExtension(
                type,
                newFormatterInfo => this.UpdateFormatterCache( type, newFormatterInfo ),
                () => this.CreateDefaultFormatter( type ) );

        this._getInvariantFormatterFunc = type =>
            (InvariantFormatterCacheEntry) Activator.CreateInstance( typeof(InvariantFormatterCacheEntry<>).MakeGenericType( type ), this )!;

        using var builder = new Builder( this );
        build?.Invoke( builder );
    }

    public static FormatterRepository Create( FormattingRole role, Action<Builder>? build = null ) => new( role, build );

    /// <inheritdoc />
    public FormattingRole Role { get; }

    /// <summary>
    /// Returns the formatter for the type <typeparamref name="T"/>. 
    /// </summary>
    public IFormatter<T> Get<T>() => (IFormatter<T>) this._invariantFormatterCache.GetOrAdd( typeof(T), this._getInvariantFormatterFunc ).GetInstance();

    /// <summary>
    /// Returns a formatter for a specific object. This overload should be used when the type of the object
    /// is not known at build time because the type is non-sealed.
    /// </summary>
    /// <param name="objectType">Object type.</param>
    /// <returns>The formatter the object <paramref name="objectType"/>.</returns>
    public IFormatter Get( Type objectType )
        => this._dynamicFormatterCache.GetOrAdd( objectType, this._getFormatterFunc ).Extension
           ?? throw new FormatterNotFoundException();

    /// <summary>
    /// Attempts to get the <see cref="IFormatter"/> for the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="objectType">Object type.</param>
    /// <param name="formatter">The formatter for objects of the specified type, or null if the repository is unable to provide a formatter for the specified type.</param>
    /// <returns>true if the repository was able to provide a formatter for the specified type; otherwise, false.</returns>
    public bool TryGet( Type objectType, out IFormatter? formatter )
    {
        formatter = this._dynamicFormatterCache.GetOrAdd( objectType, this._getFormatterFunc ).Extension;

        return formatter != null;
    }

    private void UpdateFormatterCache( Type objectType, TypeExtensionInfo<IFormatter> newFormatterInfo )
    {
        while ( true )
        {
            if ( !this._dynamicFormatterCache.TryGetValue( objectType, out var oldFormatterInfo ) )
            {
                this._dynamicFormatterCache.TryAdd( objectType, newFormatterInfo );

                return;
            }

            if ( !newFormatterInfo.ShouldOverwrite( oldFormatterInfo ) )
            {
                return;
            }

            if ( this._dynamicFormatterCache.TryUpdate( objectType, newFormatterInfo, oldFormatterInfo ) )
            {
                return;
            }
        }
    }

    private IFormatter CreateDefaultFormatter( Type type )
        => type.IsAnonymous()
            ? new AnonymousTypeFormatter( this, type )
            : (IFormatter) Activator.CreateInstance(
                typeof(DefaultFormatter<>).MakeGenericType( type ),
                this )!;

    private IFormatter CreateDefaultFormatter<T>()
        => typeof(T).IsAnonymous()
            ? new AnonymousTypeFormatter( this, typeof(T) )
            : new DefaultFormatter<T>( this );

    private abstract class InvariantFormatterCacheEntry
    {
        public abstract IFormatter GetInstance();
    }

    private sealed class InvariantFormatterCacheEntry<T> : InvariantFormatterCacheEntry
    {
        private readonly FormatterRepository _parent;
        private IFormatter<T> _formatter;
        private TypeExtensionInfo<IFormatter> _formatterInfo;

        public InvariantFormatterCacheEntry( FormatterRepository parent )
        {
            this._parent = parent;
            this._formatterInfo = parent._formatterFactory.GetTypeExtension( typeof(T), this.UpdateCacheCallback, this.CreateDefaultFormatter );

            this._formatter = (IFormatter<T>) (this._formatterInfo.Extension
                                               ?? throw new FormattersAssertionFailedException( "null was not expected." ));
        }

        private IFormatter CreateDefaultFormatter()
        {
            if ( typeof(T) == typeof(object) || typeof(T).IsInterface || typeof(T).IsAbstract )
            {
                return new DynamicFormatter<T>( this._parent );
            }
            else if ( typeof(T).IsEnum )
            {
                return (IFormatter) Activator.CreateInstance( typeof(EnumFormatter<>).MakeGenericType( typeof(T) ), this._parent )!;
            }
            else
            {
                return this._parent.CreateDefaultFormatter<T>();
            }
        }

        private void UpdateCacheCallback( TypeExtensionInfo<IFormatter> typeExtensionInfo )
        {
            if ( typeExtensionInfo.ShouldOverwrite( this._formatterInfo ) )
            {
                this._formatter = (IFormatter<T>) (this._parent._formatterFactory.Convert( typeExtensionInfo.Extension, typeof(T) )
                                                   ?? throw new FormattersAssertionFailedException( "null was not expected." ));

                this._formatterInfo = typeExtensionInfo;
            }
        }

        public override IFormatter GetInstance() => this._formatter;
    }
}