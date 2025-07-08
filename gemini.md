# Gemini Project Context: DLGameViewer

This file provides context for the Gemini agent to understand and work with this project.

## Project Overview

- **Project Name:** DLGameViewer
- **Type:** C# WPF Desktop Application
- **Description:** A viewer for games, managing a library of games from local folders. It scans folders, fetches metadata from the web, and allows users to execute games. It supports settings persistence for theme and page size.
- **Architecture:** Follows the Model-View-ViewModel (MVVM) pattern with Dependency Injection for services.

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

## Key Features & Logic

- **Folder Scanning**: The `FolderScannerService` recursively scans directories for games matching specific patterns (e.g., RJ-codes). It uses a `CancellationToken` to support cancellation from the UI.
- **Real-time Updates**: During a scan, discovered games are added to the main view in real-time. Once the scan completes, the view is refreshed to ensure all data and high-quality images are displayed correctly.
- **Settings Persistence**: User settings (theme and page size) are managed by the `SettingsService`. Settings are loaded on startup from `%APPDATA%\DLGameViewer\settings.json` and saved whenever they are changed in the UI.
- **Dependency Injection**: Services are registered in `App.xaml.cs` and injected into view models and other services, following a centralized management approach.

## Project Structure

- `DLGameViewer.sln`: The main solution file.
- `DLGameViewer.csproj`: The main C# project file.
- `App.xaml.cs`: The application entry point and DI container configuration.
- `Views/`: Contains the UI (XAML files).
  - `MainWindow.xaml`: The main application window.
  - `ScanResultDialog.xaml`: Dialog to show scan progress, which also contains the "Stop Scan" button.
- `ViewModels/`: Contains the logic for the views (MVVM pattern).
  - `MainViewModel.cs`: The primary view model. Handles application logic, including initiating scans, managing settings, and controlling UI state.
- `Models/`: Contains the data structures.
  - `GameInfo.cs`: Represents a game. Implements `INotifyPropertyChanged` for UI updates.
  - `AppSettings.cs`: Represents the user-configurable settings that are persisted.
- `Services/`: Contains business logic services.
  - `FolderScannerService.cs`: Scans folders for games.
  - `DatabaseService.cs`: Manages game data persistence in a SQLite database.
  - `WebMetadataService.cs`: Fetches game metadata from the web.
  - `SettingsService.cs`: Manages loading and saving of user settings to a JSON file.
- `Helpers/`: Contains utility classes like `ThemeManager` and `ScanProcessHelper`.

## Testing

- No dedicated test project was found. When adding tests, a new project (e.g., `DLGameViewer.Tests.csproj`) would be the standard approach.