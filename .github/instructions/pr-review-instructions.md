# GitHub Copilot Instructions for Metalama

## Code Review Guidelines

When reviewing code, focus on substantive issues only:

### DO Review
- Logic errors and bugs
- Security vulnerabilities
- Performance issues
- Missing null checks or error handling
- Incorrect API usage
- Missing or incorrect documentation
- Test coverage gaps
- Architectural concerns

### DO NOT Review
- Code formatting (handled by automated tools)
- Whitespace issues
- Brace placement
- Line length
- Naming conventions that follow existing patterns
- Import/using statement ordering
- Any issue that would be caught by the C# compiler
- Any issue that would be caught by code analyzers

## General Guidelines

- This is a C# monorepo with multiple solutions
