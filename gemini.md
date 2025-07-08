# Gemini Project Context: DLGameViewer

This file provides context for the Gemini agent to understand and work with this project.

## Project Overview

- **Project Name:** DLGameViewer
- **Type:** C# WPF Desktop Application
- **Description:** A viewer for games, likely managing a library of games from local folders. It seems to scan folders, fetch metadata, and allow users to execute games.
- **Architecture:** Follows the Model-View-ViewModel (MVVM) pattern.

## Development Environment

- **Language:** C#
- **Framework:** .NET (WPF for UI)
- **IDE:** Visual Studio (indicated by `.sln` and `.vs` files)

## Build and Run

- **Build:** To build the project, use the .NET CLI:
  ```shell
  dotnet build DLGameViewer.sln
  ```
- **Run:** To run the application:
  ```shell
  dotnet run --project DLGameViewer.csproj
  ```

## Project Structure

- `DLGameViewer.sln`: The main solution file.
- `DLGameViewer.csproj`: The main C# project file.
- `App.xaml.cs`: The application entry point.
- `Views/`: Contains the UI (XAML files).
  - `MainWindow.xaml`: The main application window.
- `ViewModels/`: Contains the logic for the views (MVVM pattern).
  - `MainViewModel.cs`: The view model for the main window.
- `Models/`: Contains the data structures.
  - `GameInfo.cs`: Represents a game.
- `Services/`: Contains business logic services.
  - `FolderScannerService.cs`: Scans folders for games.
  - `DatabaseService.cs`: Manages game data persistence.
  - `WebMetadataService.cs`: Fetches game metadata from the web.
- `Helpers/`: Contains utility classes.

## Testing

- No dedicated test project was found in the initial file listing. When adding tests, a new project (e.g., `DLGameViewer.Tests.csproj`) would be the standard approach.
