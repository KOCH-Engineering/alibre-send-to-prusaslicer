KOCH Send to PrusaSlicer - add-on for Alibre Design
===================================================
Developed by KOCH Engineering (Henrik J. Koch, Denmark)
Built with Claude (Anthropic).
https://www.linkedin.com/in/henrikjkoch/

One click sends the open part - or every part of the open assembly as
separate objects - from Alibre Design to PrusaSlicer.

HOW TO USE
1. Open a part or an assembly in Alibre Design.
2. Click  Add-on > PrusaSlicer > Send to PrusaSlicer
3. PrusaSlicer opens (or the open PrusaSlicer window gets the parts).

Tip: in PrusaSlicer tick  Preferences > "Allow just a single PrusaSlicer
instance", so new parts are added to the window you already have open.

SETTINGS  (Add-on > PrusaSlicer > Settings...)
- Where PrusaSlicer is (found automatically)
- One object per instance (4 identical bolts = 4 objects)
- Include sub-assemblies, skip suppressed / hidden parts
- File format: STL or STEP (AP214 / AP242 / AP203)
- STL mesh quality from your Alibre STL export settings
- Save the files in a temporary folder or a "Print" folder next to the model

REQUIREMENTS
- Alibre Design (64-bit), tested with version 29
- PrusaSlicer 2.4 or newer (needed for STEP)

FILES
- Add-on:   C:\ProgramData\Alibre AddOns\KochPrusaSlicer
- Settings: %APPDATA%\Koch Engineering\PrusaSlicer\settings.ini
- Log:      %LOCALAPPDATA%\Alibre AddOns\KochPrusaSlicer\addon.log

UNINSTALL
Windows Settings > Apps > KOCH Send to PrusaSlicer (Alibre add-on).

MIT License - (c) 2026 KOCH Engineering.
Alibre and Alibre Design are trademarks of Alibre, LLC. PrusaSlicer is a
trademark of Prusa Research a.s. Not affiliated with or endorsed by either.
