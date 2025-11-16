# Dragonscale Storyteller

Dragonscale Storyteller ist eine geplante Webanwendung, die PDF-Dokumente einliest und daraus automatisch eine illustrierte Geschichte erzeugt. Eine KI extrahiert die wichtigsten Informationen aus der PDF, strukturiert daraus einen mehrphasigen Story-Ablauf und erzeugt zu jeder Phase Bildprompts für ein Bildgenerationsmodell. Die fertige Geschichte mit den passenden Illustrationsprompts wird anschließend auf der Webseite dargestellt.

> **Projektstatus:** Funktionsumfang noch im Aufbau. Aktuell existiert nur die Konzeption; Upload, KI-Auswertung und Frontend-Rendering werden in den nächsten Iterationen implementiert.

## Zielbild der Anwendung
- **PDF-Eingabe**: Nutzer laden eine oder mehrere PDFs hoch, die als inhaltliche Basis dienen.
- **Informationsextraktion**: Die KI fasst relevante Fakten, Namen, Orte und Handlungspunkte aus der PDF zusammen.
- **Strukturierter Story-Aufbau**: Die Geschichte wird in klar definierte Phasen (z. B. Einleitung, Konflikt, Höhepunkt, Auflösung) gegliedert.
- **Bildprompts pro Phase**: Für jede Phase erzeugt die KI einen prägnanten Prompt, der ein passendes Bild zur Szene beschreibt.
- **Web-Darstellung**: Text und Prompts werden in der Oberfläche angezeigt; die Prompts können an einen Bildgenerator übergeben werden, um die Geschichte zu illustrieren.

## Geplanter Ablauf
1. **Upload & Vorverarbeitung**: PDFs werden entgegengenommen, in Text umgewandelt und für die KI aufbereitet.
2. **Inhaltsanalyse**: Ein LLM identifiziert Kernaussagen, Charaktere, Orte und Stimmungen.
3. **Story-Synthese**: Aus den extrahierten Informationen wird ein strukturierter Output generiert, der die Phasen der Geschichte inklusive Titel, Beschreibung und Stimmung definiert.
4. **Prompt-Generierung**: Zu jeder Phase entsteht ein Bildprompt (z. B. für Stable Diffusion oder DALL·E), der als Hilfestellung für die Illustration dient.
5. **Anzeige & Export**: Die Story mit allen Phasen und Prompts wird auf der Webseite dargestellt; optional können die Ergebnisse exportiert oder weiterverwendet werden.

## Strukturierter Output (Beispiel)
```json
{
  "title": "Die Chronik des Drachenpfads",
  "phases": [
    {
      "name": "Einleitung",
      "summary": "Die Entdecker finden in den Highlands ein altes Pergament, das einen vergessenen Drachenhort beschreibt.",
      "image_prompt": "Wide cinematic view of misty Scottish highlands at dawn, explorers holding a weathered parchment, dramatic lighting"
    },
    {
      "name": "Konflikt",
      "summary": "Im Tal der Schatten stellt sich der Gruppe ein Rivalenteam entgegen, das den Schatz zuerst beanspruchen will.",
      "image_prompt": "Tense standoff between two explorer teams in a shadowy valley, torches casting long shadows, fantasy realism"
    },
    {
      "name": "Höhepunkt",
      "summary": "Der Drache erwacht im unterirdischen Gewölbe; ein Wettlauf zwischen Mut und Furcht beginnt.",
      "image_prompt": "Ancient dragon emerging from glowing cavern, heroes silhouetted, molten gold and blue fire, dynamic action scene"
    },
    {
      "name": "Auflösung",
      "summary": "Die Gruppe gewinnt das Vertrauen des Drachen und erhält Wissen statt Gold, das die Welt verändert.",
      "image_prompt": "Peaceful scene of a dragon sharing luminous scrolls with explorers, warm light, hopeful atmosphere"
    }
  ]
}
```

## Offene Implementierungspunkte
- Upload-Flow und Textextraktion aus PDF.
- KI-gestützte Zusammenfassung und Strukturierung der Inhalte.
- Generierung von Bildprompts pro Story-Phase.
- UI für Upload, Story-Review und Ausgabe der Prompts.
- Optionale Anbindung an einen Bildgenerator.

## Lokale Entwicklung (aktuell geplant)
- Projekt basiert auf .NET (siehe `Dragonscale_Storyteller.csproj`).
- Nach Installation der .NET SDK: `dotnet restore` und `dotnet run` im Projektverzeichnis.
- Weitere Infrastruktur (z. B. Speicher für Uploads oder KI-Services) wird noch definiert.

## Lizenz
Noch nicht festgelegt.
