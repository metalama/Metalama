// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.Implementations;
using JetBrains.Annotations;
using System.Reflection;

namespace Flashtrace.Formatters;

public partial class FormatterRepository
{
    [PublicAPI]
    public sealed class Builder : IDisposable
    {
        private readonly FormatterRepository _formatters;
        private bool _disposed;

        public Builder( FormatterRepository formatters )
        {
            this._formatters = formatters;
            this.RegisterDefaultFormatters();
        }

        private void CheckNotDisposed()
        {
            if ( this._disposed )
            {
                throw new ObjectDisposedException( this.GetType().FullName );
            }
        }

        // For tests only.
        internal void Clear()
        {
            this._formatters._formatterFactory.Clear();
            this._formatters._dynamicFormatterFactory.Clear();
        }

        private void RegisterDefaultFormatters()
        {
            this.AddFormatter( typeof(string), new StringFormatter( this._formatters ) );
            this.AddFormatter( typeof(char), new CharFormatter( this._formatters ) );
            this.AddFormatter( typeof(bool), new BooleanFormatter( this._formatters ) );
            this.AddFormatter( typeof(byte), new ByteFormatter( this._formatters ) );
            this.AddFormatter( typeof(sbyte), new SByteFormatter( this._formatters ) );
            this.AddFormatter( typeof(ushort), new UInt16Formatter( this._formatters ) );
            this.AddFormatter( typeof(short), new Int16Formatter( this._formatters ) );
            this.AddFormatter( typeof(uint), new UInt32Formatter( this._formatters ) );
            this.AddFormatter( typeof(int), new Int32Formatter( this._formatters ) );
            this.AddFormatter( typeof(ulong), new UInt64Formatter( this._formatters ) );
            this.AddFormatter( typeof(long), new Int64Formatter( this._formatters ) );
            this.AddFormatter( typeof(float), new DefaultFormatter<float>( this._formatters ) );
            this.AddFormatter( typeof(double), new DefaultFormatter<double>( this._formatters ) );
            this.AddFormatter( typeof(decimal), new DefaultFormatter<decimal>( this._formatters ) );
            this.AddFormatter( typeof(UIntPtr), new DefaultFormatter<UIntPtr>( this._formatters ) );
            this.AddFormatter( typeof(IntPtr), new DefaultFormatter<IntPtr>( this._formatters ) );
            this.AddFormatter( typeof(DateTime), new DefaultFormatter<DateTime>( this._formatters ) );
            this.AddFormatter( typeof(DateTimeOffset), new DefaultFormatter<DateTimeOffset>( this._formatters ) );
            this.AddFormatter( typeof(Guid), new DefaultFormatter<Guid>( this._formatters ) );
            this.AddFormatter( typeof(TimeSpan), new DefaultFormatter<TimeSpan>( this._formatters ) );
#if NET6_0_OR_GREATER
            this.AddFormatter( typeof(ISpanFormattable), typeof(SpanFormattableFormatter<>) );
#endif
            this.AddFormatter( typeof(IFormattable<>), typeof(FormattableFormatter<,>) );
            this.AddFormatter( typeof(Nullable<>), typeof(NullableFormatter<>) );
            this.AddFormatter( typeof(Type), new TypeFormatter( this._formatters ) );
            this.AddFormatter( typeof(MethodBase), new MethodInfoFormatter( this._formatters ) );
        }

        /// <summary>
        /// Requests that formatters for parameters of a given type will be resolved according
        /// to the type of the parameter value, not to the type of the parameter itself. Interfaces, abstract classes and the <see cref="object"/> class are
        /// always resolved dynamically.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        public void SetDynamic<T>()
            where T : class
        {
            this.CheckNotDisposed();

            this._formatters._formatterFactory.RegisterTypeExtension( typeof(T), new DynamicFormatter<T>( this._formatters ) );
        }

        /// <summary>
        /// Registers the given <paramref name="formatter"/> for the type <typeparamref name="T"/>.
        /// </summary>
        public void AddFormatter<T>( IFormatter<T> formatter )
        {
            if ( formatter == null )
            {
                throw new ArgumentNullException( nameof(formatter) );
            }

            this.CheckNotDisposed();

            this.AddFormatter( typeof(T), formatter );
        }

        public void AddFormatter<T>( Func<IFormatterRepository, IFormatter<T>> formatterFactory ) => this.AddFormatter( formatterFactory( this._formatters ) );

        /// <summary>
        /// Registers the given <paramref name="formatter"/> for the given <paramref name="targetType"/>.
        /// </summary>
        /// <remarks>The formatter will work for the given target type, and also for any type that inherits/implements the target type.</remarks>
        public void AddFormatter( Type targetType, IFormatter formatter )
        {
            this.CheckNotDisposed();

            this._formatters._formatterFactory.RegisterTypeExtension( targetType, formatter );
            this._formatters._dynamicFormatterFactory.RegisterTypeExtension( targetType, formatter );
        }

        public void AddFormatter( Type targetType, Func<IFormatterRepository, IFormatter> formatterFactory )
            => this.AddFormatter( targetType, formatterFactory( this._formatters ) );

        /// <summary>
        /// Registers the given <paramref name="formatterType"/> for the given <paramref name="targetType"/>.
        /// </summary>
        /// <remarks>
        /// <para>Instances of <paramref name="formatterType"/> are going to be created using a parameterless constructor.</para>
        /// <para>When <paramref name="targetType"/> is generic, the registration applies to its generic instantiations
        /// (and types that inherit/implement them).</para>
        /// </remarks>
        /// <example>
        /// If you register a formatter as
        /// <c>FormattingServices.RegisterFormatter(typeof(IDictionary&lt;,&gt;), typeof(MyDictionaryFormatter&lt;,&gt;)</c>
        /// and then log a parameter of type <c>Dictionary&lt;int, string&gt;</c>, the formatter for that parameter will be created
        /// by calling <c>new MyDictionaryFormatter&lt;int, string&gt;</c>.
        /// </example>
        public void AddFormatter( Type targetType, Type formatterType )
        {
            this._formatters._formatterFactory.RegisterTypeExtension( targetType, formatterType );
            this._formatters._dynamicFormatterFactory.RegisterTypeExtension( targetType, formatterType );
        }

        public void Dispose()
        {
            this._disposed = true;
        }
    }
}