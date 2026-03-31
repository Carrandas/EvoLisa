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

## Architecture

- GUI layer (GA project) should be thin - business logic goes in GABase
- GABase contains: Population, Chromosome, Selector, Mutator, Evolver
- GA contains: Windows Forms UI