# Convoy Survival Groundwork

Diese Datei beschreibt nur das aktuelle Grundgeruest des Projekts.
Sie ist als schnelle Orientierung fuer alle gedacht, die auf dem bestehenden Stand weiterarbeiten.

## Zweck

Das Grundgeruest liefert:
- ein spielbares Unity-Projekt
- eine testbare Prototype-Szene
- einen First-Person-Player mit Basis-Gameplay
- einfache Interaktion
- einfache Physics-Props
- eine kleine UI fuer Hinweise und Sammelobjekte

Nicht Teil dieses Grundgeruests sind:
- finales World Design
- Zombie-KI
- Combat-System
- Audio-Polish
- Story, Questlogik oder Endgame

## Wichtige Szene

Zum Testen und Weiterbauen bitte diese Szene verwenden:

`Assets/_Project/Scenes/BrowserPrototype.unity`

Nicht versehentlich nur die Unity-Standardszene `SampleScene` verwenden.

## Aktuelle Features

- First-Person-Kamera
- Maus-Look
- Bewegung mit `WASD`
- Springen mit `Space`
- Crouch mit `Shift`
- Sprint per Doppeltipp auf `W`
- Respawn, wenn der Spieler aus der Welt faellt
- Push-Physics mit beweglichen Boxen
- Interaktionssystem mit `E`
- Collectibles mit Counter
- einfache Runtime-Performance-Defaults
- testbare Wasserflaeche mit Buoyancy fuer Physics-Objekte

## Steuerung

- `WASD` = bewegen
- `Space` = springen
- `Shift` halten = crouchen
- `W` doppeltippen = sprinten
- `E` = interagieren / Collectibles aufnehmen
- `Left Click` = Cursor locken
- `Escape` = Cursor freigeben

## Wichtige Scripts

### Player

- `Assets/_Project/Scripts/Player/BrowserFpsController.cs`

Verantwortlich fuer:
- Bewegung
- Kamera-Look
- Sprint
- Crouch
- Jumping
- Respawn
- Physics Push

### Interaktion

- `Assets/_Project/Scripts/Interaction/BrowserPrototypeInteractor.cs`
- `Assets/_Project/Scripts/Interaction/BrowserPrototypeInteractionState.cs`
- `Assets/_Project/Scripts/Interaction/BrowserPrototypeCollectible.cs`
- `Assets/_Project/Scripts/Interaction/BrowserPrototypeInteractable.cs`

Verantwortlich fuer:
- Zielerkennung
- Sichtlinien-Check
- `E`-Interaktion
- Collectibles
- Counter fuer eingesammelte Objekte

### UI

- `Assets/_Project/Scripts/UI/BrowserPrototypeUiController.cs`

Verantwortlich fuer:
- Start-Overlay
- Interaktionshinweise
- Collectible-Counter

### Runtime / Performance

- `Assets/_Project/Scripts/Runtime/BrowserPrototypeRuntimeSettings.cs`

Verantwortlich fuer:
- Target Framerate
- Shadow Distance
- einfache Laufzeit-Defaults

### Wasser / Physics

- `Assets/_Project/Scripts/Water/BrowserPrototypeWaterSurface.cs`
- `Assets/_Project/Scripts/Water/BrowserPrototypeWaterVolume.cs`
- `Assets/_Project/Scripts/Water/BrowserPrototypeBuoyancyBody.cs`

Verantwortlich fuer:
- Wasseroberflaeche
- Splash / Disturbance
- Buoyancy der Physics-Objekte

### Szene auf Knopfdruck neu erzeugen

- `Assets/_Project/Scripts/Editor/BrowserPrototypeSceneBuilder.cs`

Im Unity-Editor:
- `Tools > HTL Spiel > Create Browser Prototype Scene`

## Woran andere weiterbauen koennen

Gute naechste Bereiche fuer Teammitglieder:
- Zombie-KI und Gegnerverhalten
- World Design und Levelaufbau
- Animationen und Modelle
- Soundeffekte und Musik
- UI-Ausbau
- Health / Damage / Combat
- Menues und Game Flow

## Hinweise fuer die Teamarbeit

- Bitte immer in der Szene `BrowserPrototype.unity` testen, wenn ihr auf dem Grundgeruest aufbaut.
- Unity `.meta`-Dateien immer mit committen.
- Wenn ihr die Steuerung aendert, auch die Texte im Overlay mitziehen.
- Groessere Gameplay-Systeme moeglichst als eigene Scripts oder saubere Erweiterungen anlegen, statt den Controller chaotisch zu ueberladen.

## Kurzfazit

Das Projekt hat aktuell ein funktionierendes spielbares Fundament.
Darauf kann jetzt das eigentliche Zombie-Spiel aufgebaut werden.
