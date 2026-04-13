# Raft-Inspired Roadtrip World Guide

## 1. Weltstruktur

Die Welt ist als persistente Roadtrip-Szene mit sauberer Produktionshierarchie aufgebaut. Die Hauptstrasse ist das Rueckgrat, waehrend Regionen und Chunks die spaetere additive Erweiterung vorbereiten.

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

## 2. Stilrichtung

Das Projekt zielt auf einen Raft-aehnlichen Grafikstil:

- klar modellierte, freundliche Silhouetten
- einfache Geometrie mit abgerundeten Akzenten statt harter Blockouts
- saubere PBR-Oberflaechen
- warme Sonne, weiche Schatten, leichtes Color Grading
- natuerliche, aber leicht angehobene Farben
- postapokalyptische Weltinhalte ohne extrem duestere Horror-Lesart

Zielbild:
`Raft, aber entlang einer verlassenen Main Road`

## 3. Regionen und Hauptorte

Die Route ist in sechs gut lesbare Abschnitte gegliedert:

1. `Suburban Approach`
   ruhiger Einstieg mit Leitungen, Baumgruppen, Haeusern und ersten Wracks
2. `Service Strip`
   Tankstelle, kleiner Shop und roadside Commerce-Zone
3. `Rest Basin`
   Rastplatz mit Kiosk, Baenken, Vegetation und Vista
4. `Bridge Narrows`
   komprimierte Engstelle mit Barrieren, Bruecke und Ditch
5. `Checkpoint`
   futuristisch-postapokalyptische Quarantaene-Barriere mit Zelten und Tower
6. `Burnout Mile`
   dichter Crash-Korridor mit Final-Vista und spaeterem Druckaufbau

## 4. Materialien und Beleuchtung

### Materialien

- Asphalt:
  leicht aufgehellt, sauber lesbar, mit Patches und feinen Schaeden
- Schultern und Terrain:
  warme Sand-, Ocker- und Gruentoene mit klarer Trennung zwischen Road und Landschaft
- Gebaeude:
  cremige Fassaden, lackierte Akzentbaender, matte Daecher, dunkles Glas
- Props:
  lackiertes Metall, Holz, Beton und dezente Emission fuer Schilder oder Displays

### Licht

- warmes Spaetnachmittags-Sonnenlicht
- soft shadows statt harter Kontraste
- heller Himmel und freundlicher Fog fuer Weite
- URP Global Volume mit:
  - ACES Tonemapping
  - leichtem Bloom
  - warmer White Balance
  - subtiler Saettigungs- und Kontrastanhebung

## 5. Technische Umsetzung

Das Projekt ist auf WebGL und spaetere Skalierung vorbereitet:

- `URP` mit SRP Batcher
- additive Chunk-Struktur bleibt erhalten
- `WorldStreamingManager` fuer spaeteres Chunk-Laden
- `WorldPerformanceConfigurator` fuer globale Budgets
- instanzierungsfaehige Laufzeit-Materialien
- einfache primitive Basismodelle fuer schnelle Iteration und klare Lesbarkeit

## 6. Future Hooks

Folgende leere Gruppen sind bereits angelegt:

- `SpawnPoints`
- `FuelStops`
- `ShopStops`
- `LootAreas`
- `ZombieSpawnAreas`
- `EventTriggers`

Damit lassen sich spaeter Gameplay-Systeme sauber in die Welt haengen, ohne die Produktionsstruktur umzubauen.

## 7. Wie du den Stil weiter ausbaust

Wenn du noch naeher an Raft heran willst, sind diese Schritte am wirkungsvollsten:

1. Primitive Platzhalter schrittweise durch modular modellierte Prefabs mit leicht gerundeten Kanten ersetzen.
2. Ein kleines, konsistentes Material-Set bauen:
   Asphalt, Holz, lackiertes Metall, Beton, Vegetation, Glas.
3. Landmarken mit klaren Farbakzenten versehen, damit jede Stop-Zone sofort lesbar bleibt.
4. Vegetation weiter stilisieren:
   weiche Canopies, reduzierte Blattdetails, klare Cluster statt chaotischer Streuung.
5. Postapokalyptische Details sauber halten:
   Schrott und Zerfall nur gezielt dort, wo sie Blickfuehrung und Geschichte staerken.
