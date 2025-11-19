# 🐉 Dragonscale Storyteller

Dragonscale Storyteller ist eine **vollständig funktionsfähige** Webanwendung, die beliebige PDF-Dokumente (Bedienungsanleitungen, Zeitungsartikel, Einkaufslisten, etc.) in kreative, **illustrierte Geschichten** verwandelt. Die Anwendung nutzt **Nebius AI** für Textgenerierung und Bildgenerierung, um aus jedem Dokument eine einzigartige Geschichte mit passenden Bildern zu erstellen.

> **Projektstatus:** ✅ **Vollständig implementiert und einsatzbereit!**  
> Die Anwendung ist produktionsreif mit vollständiger PDF-Verarbeitung, AI-gestützter Story-Generierung, automatischer Bildgenerierung und professionellem Frontend.

## ✨ Features

### 📄 PDF-Verarbeitung
- **Drag & Drop Upload** oder Dateiauswahl
- Unterstützt PDFs bis 10MB
- Automatische Textextraktion mit PdfPig
- Validierung und Fehlerbehandlung

### 🎨 Story-Konfiguration
Vor dem Upload können Sie die Geschichte anpassen:
- **Sprache**: Deutsch oder Englisch
- **Stimmung**: 12 Optionen
  - Neutral, Abenteuer, Episch, Lustig, Traurig
  - Horror, Drama, Romantisch, Mysteriös
  - Inspirierend, Düster, **Kindergeschichte**
- **Keywords**: Eigene Begriffe, die in die Geschichte eingearbeitet werden

### 🤖 AI-gestützte Generierung
- **Content-Analyse**: Extraktion von Fakten, Entitäten und Konzepten
- **Story-Generierung**: 4-phasige Narrative (Einleitung, Konflikt, Höhepunkt, Auflösung)
- **Bildgenerierung**: Automatische Erstellung von Illustrationen mit Nebius AI (flux-schnell)
- **Korrekturlesen**: Alle Texte werden automatisch auf Fehler überprüft

### 📖 Professionelle Darstellung
- **Durchgehende Geschichte** wie in einem Buch
- Bilder wechseln zwischen links und rechts
- Text fließt natürlich um die Bilder
- Responsive Design für Desktop und Mobile

### 💾 Export-Funktionen
- **JSON-Export**: Vollständige Story-Daten mit Base64-kodierten Bildern
- **PDF-Export**: Professionell formatiertes PDF mit eingebetteten Bildern

## 🚀 Schnellstart

### Voraussetzungen
- .NET 10.0 SDK
- Nebius AI API Key ([Registrierung](https://nebius.ai))

### Installation

1. **Repository klonen**
```bash
git clone <repository-url>
cd Dragonscale_Storyteller
```

2. **API Key konfigurieren**

**Option A: User Secrets (empfohlen)**
```bash
cd Dragonscale_Storyteller
dotnet user-secrets set "NebiusAi:ApiKey" "your-api-key-here"
```

**Option B: appsettings.json**
```json
{
  "NebiusAi": {
    "ApiKey": "your-api-key-here"
  }
}
```

3. **Anwendung starten**
```bash
dotnet run --project Dragonscale_Storyteller
```

4. **Browser öffnen**
```
https://localhost:5001
```

### Verwendung

1. **Geschichte konfigurieren**
   - Sprache wählen (Deutsch/English)
   - Stimmung auswählen (z.B. Abenteuer, Kindergeschichte)
   - Optional: Keywords eingeben

2. **PDF hochladen**
   - Drag & Drop oder Datei auswählen
   - Warten (~1-2 Minuten für vollständige Verarbeitung)

3. **Geschichte genießen**
   - Durchgehende Geschichte mit Bildern
   - Als JSON oder PDF exportieren

## 🔄 Verarbeitungs-Pipeline

```
PDF Upload
    ↓
Text-Extraktion (PdfPig)
    ↓
Content-Analyse (Nebius AI)
    ↓
Story-Generierung (4 Phasen)
    ↓
Korrekturlesen (Titel + Phasen)
    ↓
Bildprompt-Generierung
    ↓
Korrekturlesen (Prompts)
    ↓
Bildgenerierung (flux-schnell)
    ↓
PDF-Erstellung (QuestPDF)
    ↓
Fertige Geschichte
```

**Verarbeitungszeit**: ~75-135 Sekunden (abhängig von Dokumentgröße)

## 📊 Technologie-Stack

### Backend
- **Framework**: ASP.NET Core 10.0
- **PDF-Verarbeitung**: PdfPig
- **PDF-Generierung**: QuestPDF
- **AI-Integration**: OpenAI Client Library (für Nebius AI)
- **Caching**: IMemoryCache

### Frontend
- **HTML5** mit modernem CSS3
- **Vanilla JavaScript** (ES6+)
- **Responsive Design**
- **Drag & Drop API**

### AI-Services
- **Text-Modell**: Meta-Llama-3.1-8B-Instruct-fast
- **Bild-Modell**: flux-schnell (black-forest-labs)
- **Provider**: Nebius AI

## 📁 Projektstruktur

```
Dragonscale_Storyteller/
├── Controllers/
│   └── StoryGeneratorController.cs    # API-Endpunkte
├── Services/
│   ├── NebiusAiService.cs             # AI-Integration
│   ├── PdfProcessorService.cs         # PDF-Verarbeitung
│   ├── PdfGeneratorService.cs         # PDF-Erstellung
│   ├── StoryService.cs                # Story-Pipeline
│   └── StoryStorageService.cs         # Speicherverwaltung
├── Models/
│   ├── GeneratedStory.cs              # Story-Datenmodell
│   ├── StoryPhase.cs                  # Phasen-Modell
│   └── StoryConfiguration.cs          # Konfigurations-Modell
├── wwwroot/
│   ├── index.html                     # Frontend
│   ├── app.js                         # JavaScript-Logik
│   └── styles.css                     # Styling
└── Exceptions/
    └── AiServiceException.cs          # Fehlerbehandlung
```

## 🎯 Beispiel-Output

### Input
- **Dokument**: Bedienungsanleitung für Kaffeemaschine
- **Sprache**: Deutsch
- **Stimmung**: Abenteuer
- **Keywords**: Magie, Entdeckung

### Output
```json
{
  "id": "abc123",
  "title": "Das Geheimnis der magischen Kaffeemaschine",
  "phases": [
    {
      "name": "Die Entdeckung",
      "summary": "In einer alten Küche entdeckt Emma eine mysteriöse Kaffeemaschine...",
      "mood": "mysterious",
      "imageData": "base64-encoded-image...",
      "order": 0
    },
    // ... 3 weitere Phasen
  ],
  "createdAt": "2025-11-16T10:30:00Z",
  "sourceFileName": "manual.pdf"
}
```

## 🧪 Testing

### Automatisierte Tests

```bash
# Alle Tests ausführen
dotnet test

# Nur Integration-Tests
dotnet test --filter "FullyQualifiedName~Integration"

# Konfiguration verifizieren
cd Dragonscale_Storyteller.Tests
./verify-nebius-config.ps1
```

### Test-Dokumentation
- `Dragonscale_Storyteller.Tests/MANUAL_TESTING_GUIDE.md` - Manuelle Test-Szenarien
- `Dragonscale_Storyteller.Tests/INTEGRATION_TEST_SUMMARY.md` - Test-Übersicht
- `Dragonscale_Storyteller.Tests/QUICK_START.md` - Schnellstart für Tests

## 📚 Dokumentation

### Feature-Dokumentation
- `IMAGE_GENERATION_IMPLEMENTATION.md` - Bildgenerierungs-Feature
- `STORY_CONFIGURATION_FEATURE.md` - Konfigurations-Optionen
- `CONTINUOUS_STORY_UPDATE.md` - Durchgehende Story-Darstellung
- `PROOFREADING_FEATURE.md` - Korrekturlese-Funktion

### API-Dokumentation
- `Dragonscale_Storyteller.Tests/API_KEY_CONFIGURATION.md` - API-Key-Setup

## 🔧 Konfiguration

### appsettings.json

```json
{
  "NebiusAi": {
    "ApiKey": "your-api-key",
    "BaseUrl": "https://api.studio.nebius.ai/v1/",
    "TextModel": "meta-llama/Meta-Llama-3.1-8B-Instruct-fast",
    "ImageModel": "black-forest-labs/flux-schnell"
  },
  "Storage": {
    "GeneratedStoriesPath": "wwwroot/generated-stories"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Dragonscale_Storyteller": "Information"
    }
  }
}
```

## 🐛 Fehlerbehandlung

Die Anwendung verfügt über umfassende Fehlerbehandlung:

- **PDF-Fehler**: Ungültige Formate, beschädigte Dateien, zu große Dateien
- **AI-Fehler**: Authentifizierung, Rate Limits, Service-Ausfälle
- **Netzwerk-Fehler**: Timeouts, Verbindungsprobleme
- **Speicher-Fehler**: Schreibfehler, fehlende Berechtigungen

Alle Fehler werden geloggt und dem Benutzer in verständlicher Form angezeigt.

## 🚀 Deployment

### Voraussetzungen
- .NET 10.0 Runtime
- HTTPS-Zertifikat (für Produktion)
- Nebius AI API Key

### Produktions-Build

```bash
dotnet publish -c Release -o ./publish
```

### Umgebungsvariablen

```bash
# API Key
export NebiusAi__ApiKey="your-api-key"

# Logging
export Logging__LogLevel__Default="Warning"
```

## 📈 Performance

### Verarbeitungszeiten
- **PDF-Verarbeitung**: 1-2 Sekunden
- **Content-Analyse**: 3-5 Sekunden
- **Story-Generierung**: 5-10 Sekunden
- **Korrekturlesen**: 25-35 Sekunden
- **Bildgenerierung**: 40-80 Sekunden (4 Bilder)
- **PDF-Erstellung**: 2-3 Sekunden

**Gesamt**: ~75-135 Sekunden pro Geschichte

### Optimierungen
- Memory Caching für Geschichten (24h)
- Streaming für große PDFs
- Asynchrone Verarbeitung
- Fehlertolerantes Korrekturlesen

## 🤝 Beitragen

Dieses Projekt wurde als vollständige Implementierung entwickelt. Für Verbesserungsvorschläge oder Fehlerberichte, bitte ein Issue erstellen.

## 📄 Lizenz

Noch nicht festgelegt.

## 🙏 Danksagungen

- **Nebius AI** für die AI-Services
- **PdfPig** für PDF-Verarbeitung
- **QuestPDF** für PDF-Generierung
- **OpenAI** für die Client-Library

---

**Version**: 1.0.0  
**Status**: ✅ Produktionsreif  
**Letzte Aktualisierung**: November 2025
