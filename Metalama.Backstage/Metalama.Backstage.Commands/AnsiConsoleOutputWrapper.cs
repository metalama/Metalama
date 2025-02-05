// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Spectre.Console;
using System;
using System.IO;
using System.Text;

namespace Metalama.Backstage.Commands
{
    internal class AnsiConsoleOutputWrapper : IAnsiConsoleOutput
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