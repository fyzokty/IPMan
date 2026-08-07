# Sprint 03 Hotfix 02 — CA1822

## Root cause

`ApplicationName` was implemented as an expression-bodied property returning a
constant literal:

`public string ApplicationName => "IPMan";`

It did not access instance state, so analyzer rule CA1822 recommended making it
static. Because the repository uses `TreatWarningsAsErrors=true`, the analyzer
warning stopped the build.

## Fix

Changed the property to a get-only instance auto-property:

`public string ApplicationName { get; } = "IPMan";`

This keeps the property suitable for normal WPF instance binding while satisfying
the analyzer without disabling CA1822.

## Apply

Replace:

`src/IPMan.App/ViewModels/MainWindowViewModel.cs`

Then run:

1. Build -> Clean Solution
2. Build -> Rebuild Solution
3. If successful, press F5.
