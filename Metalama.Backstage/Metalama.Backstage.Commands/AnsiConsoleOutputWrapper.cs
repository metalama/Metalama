// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Spectre.Console;
using System;
using System.IO;
using System.Text;

namespace Metalama.Backstage.Commands
{
    internal sealed class AnsiConsoleOutputWrapper : IAnsiConsoleOutput
    {
        private readonly TextWriter _underlying;

        public AnsiConsoleOutputWrapper( TextWriter underlying )
        {
            this._underlying = underlying;

            try
            {
                this.Width = Console.WindowWidth;
                this.Height = Console.WindowHeight;
            }
            catch ( IOException )
            {
                this.Width = 16 * 1024;
                this.Height = 256;
            }
        }

        void IAnsiConsoleOutput.SetEncoding( Encoding encoding ) { }

        TextWriter IAnsiConsoleOutput.Writer => this._underlying;

        public bool IsTerminal => true;

        public int Width { get; }

        public int Height { get; }
    }
}