# Send to Slicer – User guide

*KOCH Engineering · Henrik J. Koch · Denmark*

**Send to Slicer** is an add-on for Alibre Design. With one click it sends the open part, or every part of the open assembly as separate objects, to your slicer. It supports the three most common FDM slicers:

| Slicer | Program file | Notes |
|---|---|---|
| **PrusaSlicer** | `prusa-slicer.exe` (alpha/beta zips: `PrusaSlicer.exe`) | Tested with 2.9.7 and 3.0.0-alpha14 |
| **Bambu Studio** | `bambu-studio.exe` | Built on PrusaSlicer, so it works the same way |
| **OrcaSlicer** | `orca-slicer.exe` | Built on Bambu Studio, so it works the same way |

For installation, see the [README](../README.md#install).

---

## 1. The menu

In Alibre Design, go to the **Add-on** tab and click **Slicer**:

| Entry | What it does |
|---|---|
| **Send to slicer** | Exports the open part, or all parts of the open assembly, and opens them in your slicer |
| **Settings…** | Choose the slicer and how parts are exported (section 3) |
| **About…** | Version, KOCH Engineering and links |
| **Reload add-on** | Loads a newly installed version without restarting Alibre |

---

## 2. Sending parts

1. Open a **part** or an **assembly** in Alibre Design. Drawings can't be sent.
2. Click **Slicer → Send to slicer**.
3. A small window shows the progress while the parts are exported.
4. Your slicer opens, or the window you already have open receives the parts.
5. With more than one object, the add-on waits until the slicer has finished loading and then **arranges** the parts on the bed by pressing **A** in the slicer's 3D view.

**Assemblies:**
- Every part becomes its **own object** in the slicer, so you can place it or give it its own filament or extruder (e.g. on a Prusa XL/INDX or a Bambu AMS).
- Parts used several times (4 identical bolts) become **4 objects** by default. This can be turned off.
- Parts in **sub-assemblies** are included, and **suppressed** parts are skipped. Skipping **hidden** parts is optional.
- Parts are exported in their **own coordinates**, not where they sit in the assembly. The slicer places them on the bed.

If the slicer asks a question while loading (for example about multi-part objects or STEP import), simply answer it. The add-on waits and arranges afterwards.

---

## 3. Settings

### 3.1 Choosing your slicer

The **Slicer** list at the top shows every slicer the add-on has found, by name and version:

```
Automatic (newest stable version)
PrusaSlicer 2.9.7
PrusaSlicer 3.0.0-alpha14  (pre-release)
Bambu Studio 2.4.0
OrcaSlicer 2.3.1
```

The full path of the chosen slicer is shown in grey underneath. Hover over it to see all of it.

| Choice | What happens |
|---|---|
| **Automatic** | Uses the newest **stable** version: PrusaSlicer first, then Bambu Studio, then OrcaSlicer. Nothing is stored, so when you update your slicer, the new version is used automatically. |
| **A specific slicer** | That exact program is always used, including an alpha or beta, until you choose another. |

**Pre-release** versions (alpha, beta, RC, nightly, dev) are marked *(pre-release)*. They are never chosen automatically unless no stable version is installed, but you can always pick one yourself.

### 3.2 How slicers are found

Installed slicers and copies that are only unzipped (portable, alpha/beta zips) are found automatically. The add-on looks in:

1. **Windows' app list**: everything installed normally.
2. **Program Files** and `%LOCALAPPDATA%\Programs`.
3. **Programs that are running right now**: an open slicer is always found.
4. **Programs Windows has seen run**: an unzipped alpha you've started before is found even when it isn't running.
5. **"Open with" associations** for .stl/.3mf files.
6. **Typical folders** for unzipped copies: Downloads, Desktop, Documents, `C:\Tools` and `C:\Apps`, two levels deep.

Copies outside Program Files are **remembered**, so they stay in the list. When a folder is deleted, the entry disappears again by itself.

**Buttons:**
- **Find** searches again and opens the list. Tip: start the slicer you're missing, then click Find.
- **Browse…** points to a slicer program yourself, for anything the search can't find. It's remembered too.

### 3.3 The other settings

| Setting | Default | Meaning |
|---|---|---|
| Arrange the parts on the bed | on | With more than one object, **A** is pressed in the slicer's 3D view once loading has finished |
| One object per instance | on | 4 identical bolts give 4 objects. Off means one object per unique part |
| Include parts in sub-assemblies | on | |
| Skip suppressed parts | on | |
| Skip hidden parts | off | |
| File format | STL | **STL**, or **STEP AP214 / AP242 / AP203**. With STEP, the slicer builds the mesh itself from the exact geometry, which gives smooth curves |
| Use my STL export settings | on | STL only: the mesh quality from Alibre's own STL export settings |
| Save the files in | Temporary folder | Or a `Print\<model>` folder next to the model, so the files are kept. This isn't available for files opened from the PDM vault, which have no Windows folder |

Each send removes the files from the previous send of the same model, so old parts never turn up.

### 3.4 The icon follows your slicer

The **Slicer** button and the About window show the logo of the slicer you've chosen, with the orange KOCH arrow pointing into it. When you change slicer:
- **About** shows the new logo immediately.
- **The ribbon button** changes the next time Alibre Design starts, because Alibre only reads the icon at startup.

If no slicer is found, the add-on's own printer icon is used.

---

## 4. Adding to the open slicer window

To have new parts go into the slicer window you already have open, instead of a new window:

| Slicer | What to do |
|---|---|
| PrusaSlicer | **Preferences → "Allow just a single PrusaSlicer instance"**. The add-on also starts PrusaSlicer with `--single-instance`. |
| Bambu Studio | **Preferences → "Keep only one Bambu Studio instance"** |
| OrcaSlicer | the corresponding "only one instance" preference |

Bambu Studio has a known issue where it sometimes ignores files sent to a window that's already open. If that happens, close Bambu Studio and send again.

---

## 5. Troubleshooting

| Symptom | What to try |
|---|---|
| My slicer isn't in the list | Start it, then click **Find**. Otherwise use **Browse…** |
| The parts lie on top of each other | Make sure **Arrange** is ticked. If the slicer showed a question, answer it, and the add-on arranges afterwards. You can always press **A** in the slicer yourself |
| A new slicer window opens every time | Turn on the "only one instance" preference (section 4) |
| "This is a drawing" | Open the part or assembly instead |
| The button still shows the old logo | Restart Alibre Design (section 3.4) |
| Anything else | Look at `%LOCALAPPDATA%\Alibre AddOns\KochPrusaSlicer\addon.log` and attach it to a [GitHub issue](../../issues) |

---

## 6. Files

| What | Where |
|---|---|
| Add-on | `C:\ProgramData\Alibre AddOns\KochPrusaSlicer` |
| Settings | `%APPDATA%\Koch Engineering\PrusaSlicer\settings.ini` |
| Exported files (temporary) | `%TEMP%\KOCH PrusaSlicer\<model>` |
| Log | `%LOCALAPPDATA%\Alibre AddOns\KochPrusaSlicer\addon.log` |

The folder names still say "PrusaSlicer" because the add-on started as *Send to PrusaSlicer*. This keeps your settings when you update.

---

## 7. Uninstalling

Use **Windows Settings → Apps → KOCH Send to Slicer (Alibre add-on)**. Your settings file is kept.

---

*Send to Slicer is developed by KOCH Engineering (Henrik J. Koch, Denmark). Built with Claude (Anthropic). Questions and ideas are welcome in the [Alibre forum](https://www.alibre.com/forum/index.php?threads/three-free-add-ons-for-alibre-design-%E2%80%93-export-parts-send-to-prusaslicer-3dconnexion-keys.27260/) or as a GitHub issue. If it saves you time, you can [buy me a coffee ☕](https://buymeacoffee.com/henrikkoch).*
