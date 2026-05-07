# Umbra

Umbra is a Unity project built with Unity `6000.4.5f1`.

## Requirements

- Unity `6000.4.5f1` or a compatible Unity 6 editor
- Git

## Opening the Project

1. Clone the repository.
2. Open the repository root folder in Unity Hub.
3. Let Unity restore packages from `Packages/manifest.json`.
4. Open one of the scenes from `Assets/Scenes`.

## Project Structure

- `Assets/` - game assets, scenes, scripts, UI, art, and settings.
- `Packages/` - Unity package manifest and lock file.
- `ProjectSettings/` - Unity project settings.

## Version Control Notes

Generated Unity folders such as `Library/`, `Temp/`, `Logs/`, `UserSettings/`, IDE project files, and local recovery files are ignored and should not be committed.

Large binary assets are kept as regular Git files for now because this project currently has no files over GitHub's 100 MB file limit and Git LFS is not installed in this environment.
