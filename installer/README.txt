KOCH Send to Slicer - add-on for Alibre Design
==============================================
Developed by KOCH Engineering (Henrik J. Koch, Denmark)
Built with Claude (Anthropic).
https://www.linkedin.com/in/henrikjkoch/
https://github.com/KOCH-Engineering/alibre-send-to-slicer

One click sends the open part - or every part of the open assembly as
separate objects - from Alibre Design to PrusaSlicer, Bambu Studio or
OrcaSlicer, and arranges them on the bed.

HOW TO USE
1. Open a part or an assembly in Alibre Design.
2. Click  Add-on > Slicer > Send to slicer
3. Your slicer opens (or the open slicer window gets the parts).

Tip: turn on the slicer's "only one instance" preference, so new parts are
added to the window you already have open.

SETTINGS  (Add-on > Slicer > Settings...)
- Your slicer: Automatic (newest stable) or a specific version - installed
  and unzipped/portable copies (incl. alpha/beta) are found automatically
- Arrange the parts on the bed after loading
- One object per instance (4 identical bolts = 4 objects)
- Include sub-assemblies, skip suppressed / hidden parts
- File format: STL or STEP (AP214 / AP242 / AP203)
- Save the files in a temporary folder or a "Print" folder next to the model

REQUIREMENTS
- Alibre Design (64-bit), tested with version 29
- PrusaSlicer 2.4+, Bambu Studio or OrcaSlicer

FILES
- Add-on:   C:\ProgramData\Alibre AddOns\KochPrusaSlicer
- Settings: %APPDATA%\Koch Engineering\PrusaSlicer\settings.ini
- Log:      %LOCALAPPDATA%\Alibre AddOns\KochPrusaSlicer\addon.log

UNINSTALL
Windows Settings > Apps > KOCH Send to Slicer (Alibre add-on).

MIT License - (c) 2026 KOCH Engineering.
Alibre and Alibre Design are trademarks of Alibre, LLC. PrusaSlicer is a
trademark of Prusa Research a.s., Bambu Studio of Bambu Lab. Not affiliated with or endorsed by either.
