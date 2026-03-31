# AGENTS.md

This file contains instructions for AI assistants working on the EvoLisa project.

## Development Workflow

- All features and fixes should be developed in a branch named `feature/<feature>`
- Do not commit directly - the user will handle commits

## Code Style

- Do not add comments unless explicitly requested
- Keep responses concise (1-3 sentences unless detail is requested)
- Follow existing code conventions in the codebase

## Testing

- Build: `dotnet build`
- Run tests if available: `dotnet test`
- Verify changes work correctly
- Never commit secrets or keys to the repository

## Testing Structure

- Test project naming: `<ProjectUnderTest>Tests`
- Test file naming: `<ClassName>Tests.cs` (e.g., `PopulationTests.cs`)
- Namespace: `<ProjectUnderTest>Tests.<ClassName>Tests` (e.g., `GABaseTests.PopulationTests`)
- Test naming: `<MethodToTest>_<TestCase>_<ExpectedResult>`
  - e.g., `Constructor_WithMaximumSize_SetsMaximumSizeProperty`
  - e.g., `Clone_WithExistingChromosomes_CreatesIndependentCopy`

## Architecture

- GUI layer (GA project) should be thin - business logic goes in GABase
- GABase contains: Population, Chromosome, Selector, Mutator, Evolver
- GA contains: Windows Forms UI