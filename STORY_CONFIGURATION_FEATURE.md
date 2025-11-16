# Story-Konfiguration Feature

## Übersicht

Die Anwendung wurde erweitert, um Geschichten vor dem Upload zu konfigurieren. Außerdem wurden die Bilder kleiner gemacht und mit Fließtext umflossen.

## Implementierte Features

### 1. ✅ Kleinere Bilder mit Fließtext

**Vorher**: Bilder waren 100% Breite und blockierten den gesamten Textfluss

**Nachher**:
- Bilder sind 300px breit
- Bilder floaten links
- Text fließt um die Bilder herum
- Auf mobilen Geräten: Bilder werden 100% Breite (gestapelt)

**CSS-Änderungen**:
```css
.phase-image-container {
    float: left;
    width: 300px;
    margin: 0 1.5rem 1rem 0;
}

.phase-summary {
    text-align: justify;  /* Blocksatz für besseren Textfluss */
}
```

### 2. ✅ Konfigurations-UI

**Neue Konfigurations-Sektion** vor dem Upload-Bereich:

#### Sprache
- **Deutsch** (Standard)
- **English**

#### Stimmung
- **Neutral** (Standard)
- **Lustig** - Humorvolle, heitere Geschichte
- **Traurig** - Melancholische, emotionale Geschichte
- **Horror** - Gruselige, spannende Geschichte
- **Drama** - Intensive, dramatische Geschichte

#### Keywords (Optional)
- Freitextfeld für kommagetrennte Keywords
- Beispiel: "Abenteuer, Magie, Freundschaft"
- Keywords werden in die Geschichte eingearbeitet

### 3. ✅ Backend-Integration

**Neues Modell**: `StoryConfiguration.cs`
```csharp
public class StoryConfiguration
{
    public string Language { get; set; } = "de";
    public string Mood { get; set; } = "neutral";
    public List<string> Keywords { get; set; } = new();
}
```

**Controller-Änderungen**:
- Akzeptiert zusätzliche Form-Parameter: `language`, `mood`, `keywords`
- Parst Keywords (kommagetrennt)
- Übergibt Konfiguration an StoryService

**Service-Änderungen**:
- `IStoryService.CreateStoryFromPdfAsync()` akzeptiert `StoryConfiguration`
- `IAiService.GenerateStoryAsync()` akzeptiert `StoryConfiguration`
- Konfiguration wird in AI-Prompts eingebaut

### 4. ✅ AI-Prompt-Anpassung

Die Konfiguration beeinflusst die Story-Generierung:

#### Sprache
```
Deutsch: "Schreibe die Geschichte auf Deutsch."
English: "Write the story in English."
```

#### Stimmung
**Lustig**:
- DE: "Die Geschichte soll lustig, heiter und humorvoll mit komödiantischen Elementen sein."
- EN: "The story should be funny, lighthearted, and humorous with comedic elements."

**Traurig**:
- DE: "Die Geschichte soll traurig, melancholisch und emotional berührend sein."
- EN: "The story should be sad, melancholic, and emotionally touching."

**Horror**:
- DE: "Die Geschichte soll gruselig, spannend sein und ein Gefühl von Angst und Horror erzeugen."
- EN: "The story should be scary, suspenseful, and create a sense of dread and horror."

**Drama**:
- DE: "Die Geschichte soll dramatisch, intensiv und emotional kraftvoll mit hohen Einsätzen sein."
- EN: "The story should be dramatic, intense, and emotionally powerful with high stakes."

#### Keywords
```
"Baue diese Schlüsselwörter in die Geschichte ein: Abenteuer, Magie, Freundschaft"
"Incorporate these keywords into the story: Adventure, Magic, Friendship"
```

### 5. ✅ Frontend-Änderungen

**HTML**:
- Neue Konfigurations-Sektion mit Grid-Layout
- Dropdown für Sprache
- Dropdown für Stimmung
- Textfeld für Keywords mit Hinweistext

**JavaScript**:
- Liest Konfigurationswerte aus
- Sendet sie als FormData mit dem Upload
- Keine Änderung am Upload-Flow

**CSS**:
- Professionelles Grid-Layout für Konfiguration
- Responsive Design (mobile: 1 Spalte)
- Hover-Effekte für Inputs
- Focus-States für bessere UX

## UI-Design

### Desktop-Ansicht
```
┌─────────────────────────────────────────┐
│  Geschichte konfigurieren               │
├─────────────────┬───────────────────────┤
│ Sprache         │ Stimmung              │
│ [Deutsch ▼]     │ [Neutral ▼]           │
├─────────────────────────────────────────┤
│ Keywords (optional)                     │
│ [z.B. Abenteuer, Magie, Freundschaft]  │
└─────────────────────────────────────────┘
```

### Story-Anzeige mit Fließtext
```
┌─────────────────────────────────────────┐
│  Phase Name                             │
├─────────────────────────────────────────┤
│  ┌────────┐                             │
│  │        │  Lorem ipsum dolor sit      │
│  │ Image  │  amet, consectetur          │
│  │        │  adipiscing elit. Sed       │
│  └────────┘  do eiusmod tempor          │
│              incididunt ut labore       │
│              et dolore magna aliqua.    │
└─────────────────────────────────────────┘
```

## Beispiel-Verwendung

### Beispiel 1: Deutsche Horror-Geschichte
```
Sprache: Deutsch
Stimmung: Horror
Keywords: Nebel, Dunkelheit, Geheimnis
```

**Ergebnis**: Gruselige Geschichte auf Deutsch mit Nebel, Dunkelheit und Geheimnissen

### Beispiel 2: Englische lustige Geschichte
```
Language: English
Mood: Happy
Keywords: Coffee, Adventure, Friends
```

**Result**: Funny story in English about coffee adventures with friends

### Beispiel 3: Deutsches Drama
```
Sprache: Deutsch
Stimmung: Drama
Keywords: Verlust, Hoffnung, Neuanfang
```

**Ergebnis**: Dramatische Geschichte über Verlust, Hoffnung und Neuanfang

## Technische Details

### API-Endpunkt

**POST** `/api/storygenerator/upload`

**Form-Data**:
```
file: [PDF-Datei]
language: "de" | "en"
mood: "neutral" | "happy" | "sad" | "horror" | "dramatic"
keywords: "keyword1, keyword2, keyword3" (optional)
```

### Datenfluss

```
Frontend Config → FormData → Controller → StoryConfiguration
                                              ↓
                                         StoryService
                                              ↓
                                         AI Service
                                              ↓
                                    Angepasste Prompts
                                              ↓
                                      Nebius AI API
                                              ↓
                                   Konfigurierte Story
```

### Logging

Alle Konfigurationsschritte werden geloggt:
```
[INFO] Story configuration: Language=de, Mood=horror, Keywords=Nebel, Dunkelheit
[INFO] Starting story generation from analysis with config: Language=de, Mood=horror
```

## Responsive Design

### Desktop (> 768px)
- 2-Spalten-Grid für Sprache und Stimmung
- Keywords über volle Breite
- Bilder 300px breit, links gefloatet

### Mobile (≤ 768px)
- 1-Spalten-Layout für Konfiguration
- Bilder 100% Breite (gestapelt)
- Kein Float, bessere Lesbarkeit

## Validierung

### Frontend
- Sprache: Nur "de" oder "en"
- Stimmung: Nur vordefinierte Werte
- Keywords: Optional, werden getrimmt

### Backend
- `StoryConfiguration` mit DataAnnotations
- RegEx-Validierung für Sprache
- Keywords werden gefiltert (leere entfernt)

## Bekannte Einschränkungen

1. **Keywords**: Werden als Vorschlag an AI gesendet, keine Garantie für Verwendung
2. **Stimmung**: AI interpretiert die Anweisung, kann variieren
3. **Sprache**: Funktioniert gut, aber AI kann gelegentlich mischen

## Zukünftige Verbesserungen

- [ ] Story-Länge konfigurierbar (kurz, mittel, lang)
- [ ] Zielgruppe (Kinder, Jugendliche, Erwachsene)
- [ ] Genre-Auswahl (Fantasy, Sci-Fi, Krimi, etc.)
- [ ] Erzählperspektive (Ich, Er/Sie, Du)
- [ ] Vorschau der Konfiguration vor Upload
- [ ] Gespeicherte Konfigurationen (Presets)

## Testing

### Manuelle Tests

1. **Standard-Konfiguration**:
   - Keine Änderungen → Deutsch, Neutral, keine Keywords
   - Upload PDF → Story sollte neutral auf Deutsch sein

2. **Englische Geschichte**:
   - Sprache: English
   - Upload PDF → Story sollte auf Englisch sein

3. **Horror-Stimmung**:
   - Stimmung: Horror
   - Upload PDF → Story sollte gruselig sein

4. **Mit Keywords**:
   - Keywords: "Magie, Drachen, Abenteuer"
   - Upload PDF → Story sollte diese Begriffe enthalten

5. **Responsive**:
   - Desktop: 2-Spalten-Layout
   - Mobile: 1-Spalte, gestapelte Bilder

## Zusammenfassung

✅ **Bilder**: Kleiner (300px) und mit Fließtext umflossen
✅ **Sprache**: Deutsch oder Englisch wählbar
✅ **Stimmung**: 5 Optionen (Neutral, Lustig, Traurig, Horror, Drama)
✅ **Keywords**: Optional, kommagetrennt
✅ **UI**: Professionell, responsive, benutzerfreundlich
✅ **Integration**: Vollständig in Backend und AI-Prompts integriert
✅ **Build**: Erfolgreich kompiliert

---

**Implementiert am**: 16. November 2025  
**Status**: ✅ ABGESCHLOSSEN
