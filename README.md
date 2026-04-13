# Roadtrip Zombie Survival World Prototype

Unity-URP-Projekt fuer eine stilisierte, hochwertige Roadtrip-Welt im Geist von Raft: klare Formen, warme Beleuchtung, saubere Materialien und lesbare POIs entlang einer verlassenen Hauptstrasse.

## Stilziel

- stylized-realistic statt grimdark
- mittlere Detaildichte mit weichen, lesbaren Silhouetten
- warme Sonne, sanfter Fog und leicht verstaerkte Naturfarben
- saubere PBR-Materialien ohne ueberladene Texturen
- postapokalyptische Inhalte, aber freundlicher Survival-Look

## Hauptstruktur

```text
World
  Terrain
  Road
  Environment
  Buildings
  Vehicles
  Props
  Lighting
  StreamingSystem
  Regions
  EventZones
  FutureGameplayHooks
```

## Wichtige Orte

- `Suburban Approach`
  fruehe verlassene Haeuser, Leitungen, erste Wracks und ruhige Einfahrt
- `Service Strip`
  Tankstelle, Shop, Ladepylon und breite Roadside-Lesbarkeit
- `Rest Basin`
  Rastplatz mit Kiosk, Bench-Cluster und Aussichtspunkt
- `Bridge Narrows`
  Bruecken-Engstelle, Barrieren und kontrollierter Funnel
- `Checkpoint`
  stilisierte Quarantaene-Barriere mit Tower, Zelten und Kontrolllinien
- `Burnout Mile`
  Crash-Korridor, Aussichtspunkt und Spaetspiel-Landmarken

## Render- und Look-Setup

- `URP` mit globalem Volume-Profile
- moderates Bloom, ACES Tonemapping, leichte Warmton-Balance
- SRP Batcher aktiv
- WebGL-freundliche Einstellungen:
  wenige Echtzeitlichter, keine schweren Effekte, limitierte Shader-Komplexitaet

Wichtige Setup-Dateien:

- `/Users/eliaseder/Codex/unity-webgl-roadtrip/Assets/Scripts/Editor/RoadtripUrpSetup.cs`
- `/Users/eliaseder/Codex/unity-webgl-roadtrip/Assets/Settings/Rendering/RoadtripUrp.asset`
- `/Users/eliaseder/Codex/unity-webgl-roadtrip/Assets/Settings/Rendering/RoadtripRoadsideVolume.asset`

## Produktions-Workflow

1. Projekt in Unity `2022.3.20f1` oeffnen.
2. Szene laden:
   `/Users/eliaseder/Codex/unity-webgl-roadtrip/Assets/Scenes/PostApocRoadtrip.unity`
3. `Tools > Roadtrip World > Setup URP Roadtrip Look`
4. `Tools > Roadtrip World > Rebuild World`
5. Optional: `Tools > Roadtrip World > Scaffold Additive Chunk Scenes`
6. Danach chunkweise Terrain, POIs und Art-Pass weiter verdichten.

## WebGL lokal testen

Der aktuelle Build liegt unter:

- `/Users/eliaseder/Codex/unity-webgl-roadtrip/Builds/WebGL`

Lokal serven:

```bash
cd /Users/eliaseder/Codex/unity-webgl-roadtrip
./scripts/serve_webgl.sh
```

Dann im Browser:

- `http://localhost:8080`

## Nächste Ausbau-Schritte

- Road-Mesh gegen finale kurvige Strecken-Geometrie tauschen
- Landmarken und POIs als Prefabs mit sauberem LOD ausbauen
- mehr Raft-nahe Materialsets fuer Holz, lackiertes Metall, Asphalt und Vegetation anlegen
- Fahrzeug-Gameplay, Looting, Zombies und Event-Systeme spaeter auf die vorhandenen Hooks setzen
