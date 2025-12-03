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

## Test Code Guidelines

When reviewing test code, be lenient:
- Do not suggest any nit
- Do not require extensive assertions for every edge case
- Do not require detailed test method documentation
- Do not suggest additional test cases unless there's an obvious gap
- Accept simple, focused tests that verify the main functionality
- Do not enforce strict naming conventions for test methods

## General Guidelines

- This is a C# monorepo with multiple solutions
