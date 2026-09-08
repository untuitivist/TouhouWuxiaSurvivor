# Game Localization

The same C# game supports Simplified Chinese (default) and English. Open Settings and use the language selector in the header. The selection is saved in the existing profile; old profiles retain their records and default to Chinese.

## Text ownership

- Keep simulation data and stable upgrade IDs language-neutral. Presentation uses Rebirth.Core.GameText.Get for source messages and GameText.Format for interpolated messages. Translate complete templates, not individual substrings.
- GameText.Catalog.cs stores English translations and concise Chinese revisions. Retain every placeholder and numeric format when adding an entry. Missing entries fall back to the original text rather than disappearing.
- GameText.Source.cs bridges preformatted core catalog descriptions using anchored whole-message templates, bounded recursion, non-backtracking patterns and a bounded cache. New UI text should use explicit Format calls instead of relying on this bridge.
- Journal data is cached per active language and rebuilt on a language change. Changing language never starts a run, resumes combat, rerolls offers or resets settings.
- Menu copy can have atmosphere. Buttons and mechanical descriptions should state the action, requirement and effect directly. Scene wards have no overlaid text; retain their artwork and progress ring. The objective panel stays; a future quest system is separate work.
- Historical release notes remain in their original Chinese. The release-notes controls are localized. The separate download portal is not translated. Language support is included in the alpha-0.1.6 Windows/Web release; switching the setting does not itself update game binaries or deploy a new build.

## Validation

Run dotnet run --project tests/localization/Localization.Tests.csproj and dotnet build --no-restore. Run Godot with --headless --path . --audio-driver Dummy -- --rebirth-language-smoke for profile, switching, journal, keyboard/touch layout and paused-run checks. Diagnostic screenshots accept --rebirth-language=en alongside the existing capture flags. These flags use isolated diagnostic profiles.
