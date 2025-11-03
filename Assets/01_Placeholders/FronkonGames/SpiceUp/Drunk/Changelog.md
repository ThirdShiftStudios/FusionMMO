## [2.1.3] - 29-08-2025

# Fix
- Fixed VR build compatibility in Unity 6.

## [2.1.2] - 22-04-2025

# Fix
- Camera 'Post Processing' checkbox fixed.

## [2.1.1] - 11-03-2025

# Fix
- Color precision fix.

## [2.1.0] - 01-03-2025

# Added
- Support for Unity 6 Render Graph.
- Support for effects in multiples Renderers.
- GetAllSettings() added to get all settings of all effects in the pipeline.
- 'Affect the Scene View' added to Unity 6.

# Removed
- Removed GetSettings(), use .Instance.settings

# Fix
- Errors when domain reload is disabled.
- Memory leak in Unity 2022.3.

## [2.0.0] - 10-01-2025

# Added
- Support for Unity 6.

# Changed
- Removed support for Unity 2021.3.
- pow() replaced by SafePositivePow() to improve compatibility.
- Performance improvements.

# Fix
- GUID collisions.
- Static variables reset when Domain Reload is disabled.

## [1.2.0] - 21-07-2024

# Changed
- Removed the AddRenderFeature() and RemoveRenderFeature() from the effect that damaged the configuration file.
- Performance improvements.

# Fix
- Small fixes.

## [1.1.1] - 02-06-2024

# Changed
- New online documentation.

## [1.1.0] - 17-10-2023

# Fixes
- Unity 2022.1 or newer support.

## [1.0.1] - 25-05-2023

# Fixes
- VR fixes.

## [1.0.0] - 12-03-2023

- Initial release.