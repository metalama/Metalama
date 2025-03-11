// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Reflection
{
    /// <summary>
    /// Not exposed in Metalama.
    /// </summary>
    [PublicAPI]
    public class SymbolSequencePoint :
        IComparable<SymbolSequencePoint>, IEquatable<SymbolSequencePoint>
    {
        public static readonly SymbolSequencePoint Hidden;

        public bool IsHidden { get; }

        public bool IsSpecial { get; }

        public int StartLine { get; }

        public int EndLine { get; }

        public int StartColumn { get; }

        public int EndColumn { get; }

        internal int Offset { get; }

        public ISourceDocument SourceDocument { get; }

        public int CompareTo( SymbolSequencePoint other )
        {
            throw new NotImplementedException();
        }

        #region IEquatable<SymbolSequencePoint> Members

        public bool Equals( SymbolSequencePoint other )
        {
            throw new NotImplementedException();
        }

        public override bool Equals( object obj )
        {
            throw new NotImplementedException();
        }

        public static bool operator ==( SymbolSequencePoint left, SymbolSequencePoint right )
        {
            throw new NotImplementedException();
        }

        public static bool operator !=( SymbolSequencePoint left, SymbolSequencePoint right )
        {
            throw new NotImplementedException();
        }

        public static bool operator <( SymbolSequencePoint left, SymbolSequencePoint right )
        {
            throw new NotImplementedException();
        }

        public static bool operator <=( SymbolSequencePoint left, SymbolSequencePoint right )
        {
            throw new NotImplementedException();
        }

        public static bool operator >( SymbolSequencePoint left, SymbolSequencePoint right )
        {
            throw new NotImplementedException();
        }

        public static bool operator >=( SymbolSequencePoint left, SymbolSequencePoint right )
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}