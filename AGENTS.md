# AGENTS.md - EvoLisa Development Guidelines

This document provides guidance for agentic coding agents working in this repository.

## Project Overview

EvoLisa is a C# genetic algorithm application that evolves images to match a target picture. The solution contains:
- **GA**: Windows Forms GUI application
- **GABase**: Core genetic algorithm library
- **GaBaseTests**: MSTest unit tests

## Build Commands

### Prerequisites
- .NET Framework 4.6.1 or higher
- Visual Studio 2019+ or MSBuild 14+

### Build All Projects
```powershell
# Using MSBuild (from Visual Studio Developer Command Prompt)
msbuild GA.sln

# Build specific configuration
msbuild GA.sln /p:Configuration=Release /p:Platform="Any CPU"
```

### Single Project Build
```powershell
msbuild GABase\GABase.csproj
msbuild GA\GAGUI.csproj
```

## Test Commands

### Run All Tests
```powershell
# Using MSTest via VSTest.Console
vstest.console.exe GaBaseTests\GABaseTests.dll
```

### Run Single Test
```powershell
# Run specific test method
vstest.console.exe GaBaseTests\GABaseTests.dll /Tests:UnitTest1.TestMethod1

# Or using mstest directly (older VS)
mstest /testcontainer:GaBaseTests\GABaseTests.dll /test:UnitTest1.TestMethod1
```

### Run Tests in Visual Studio
Open the solution in Visual Studio and use Test Explorer (Test > Test Explorer).

## Code Style Guidelines

### General Conventions
- Target Framework: .NET Framework 4.6.1
- Use 4 spaces for indentation (no tabs)
- Maximum line length: 120 characters
- Use Windows-style line endings (CRLF)

### Naming Conventions
- **Classes/Interfaces**: PascalCase (e.g., `Chromosome`, `IPopulation`)
- **Methods**: PascalCase (e.g., `GenerateRandomChromosome`)
- **Properties**: PascalCase (e.g., `MaximumSize`, `IsDirty`)
- **Private fields**: camelCase with underscore prefix (e.g., `_size`)
- **Parameters**: camelCase (e.g., `maximumSize`)
- **Constants**: PascalCase (e.g., `MinPolygonPointCount`)
- **Namespaces**: PascalCase (e.g., `GABase.Tools`)

### File Organization
- One public class per file (filename matches class name)
- Order: using statements > namespace > class declaration > fields > constructors > properties > methods
- Group related classes in subdirectories (e.g., `GABase/Tools/`, `GABase/ChromosomeChanged/`)

### Imports
- Use explicit namespaces (no global using statements in older .NET)
- Order: System.* > Microsoft.* > project namespaces
- Group using statements with #region if needed (see Population.cs example)

### Types
- Use explicit types except for var with clear initialization
- Prefer `List<T>` over arrays for collections
- Use `PixelFormat.Format32bppArgb` for bitmap operations
- Use `System.Drawing` types (Point, Rectangle, Color, Bitmap)

### Error Handling
- Use try-catch blocks for operations that may fail (file I/O, image processing)
- Log errors appropriately (no external logging library observed)
- Handle nullable types with null checks

### Performance Considerations
- `FastBitmap.cs` uses unsafe code for pixel manipulation - verify `AllowUnsafeBlocks` is enabled
- Use `using` statements for Graphics, Bitmap, and Brush objects
- Consider bounding box checks before drawing (see Population.GetPicture overloads)

### UI Guidelines (Windows Forms)
- Designer-generated code goes in `*.Designer.cs` files
- Use partial classes for forms (e.g., `GAGUI.cs` + `GAGUI.Designer.cs`)
- Resources go in `.resx` files

### Testing
- Use MSTest framework (`Microsoft.VisualStudio.TestTools.UnitTesting`)
- Place tests in `GaBaseTests` project
- Test naming: `<ClassName>_<MethodName>_<Scenario>` or similar pattern

## Project Structure

```
EvoLisa/
├── GA.sln
├── GA/                    # Windows Forms GUI
│   ├── GAGUI.cs
│   ├── Program.cs
│   └── Properties/
├── GABase/                # Core library
│   ├── Chromosome.cs
│   ├── Population.cs
│   ├── Mutator.cs
│   ├── Selector.cs
│   ├── Settings.cs
│   ├── Tools/
│   │   ├── ColorHandler.cs
│   │   ├── DifferencePicture.cs
│   │   ├── FastBitmap.cs
│   │   └── RandomGenerator.cs
│   └── ChromosomeChanged/
└── GaBaseTests/           # Unit tests
    └── UnitTest1.cs
```

## Common Tasks

### Adding a New Class
1. Add to appropriate folder in GABase or GA project
2. Add to corresponding .csproj file if not auto-detected
3. Follow naming and organization conventions above

### Running the Application
```powershell
# Build in Release mode
msbuild GA.sln /p:Configuration=Release

# Run the executable
.\GA\bin\Release\GA.exe
```

### Debugging
- Attach Visual Studio debugger to GA.exe
- Set breakpoints in GABase library code
- Use Diagnostic Tools for performance profiling
