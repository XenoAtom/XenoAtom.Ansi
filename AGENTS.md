# XenoAtom.Ansi instructions

Use end of line to CRLF on Windows and LF on Linux/Mac.

## Overview

- In the `readme.md` file, you will find general information about the XenoAtom.Ansi project.
- In the `doc/readme.md` file you will find the documentation of XenoAtom.Ansi.
- In the `samples` folder, you will find example usages of XenoAtom.Ansi.

## Project Structure

- In the `src/XenoAtom.Ansi` folder you will find the code of the library.
- In the `src/XenoAtom.Ansi.Tests` folder you will find the unit tests of the library.

## Building and Testing

- To build the project, navigate to the `src` directory and run `dotnet build`.
- To run the unit tests, navigate to the `src` directory and run `dotnet test`.
- Ensure that all tests pass successfully before submitting any changes.
- Ensure that documentation and front readme are updated to reflect any changes made to library.

## General Coding Instructions

- Follow the coding style and conventions used in the existing code base.
- Write clear and concise comments to explain the purpose and functionality of your code.
- Ensure that your code is well-structured and modular to facilitate maintenance and future enhancements.
- Adhere to best practices for error handling and input validation.
- Write unit tests for any new functionality you add to ensure code quality and reliability.
  - When fixing a bug, add a unit test that reproduces the bug, verify that it is actually failing before implementing the fix.
- Use meaningful variable and method names that accurately reflect their purpose.
- Avoid code duplication by reusing existing methods and classes whenever possible.

## Performance Considerations

- Ensure that the code is optimized for performance without sacrificing readability.
- Ensure that the code minimizes GC allocations where possible.
  - Use `Span<T>`/`ReadOnlySpan<T>` where appropriate to reduce memory allocations.

## Git Commit Instructions

- Write a concise and descriptive commit message that summarizes the changes made.
- Create a commit for each logical change or feature added to facilitate easier code review and tracking of changes.