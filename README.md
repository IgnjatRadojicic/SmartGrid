# Smart Grid Monitor

Sistem za pracenje i analizu stanja pametne elektroenergetske mreze (Smart Grid) zasnovan na WCF servisima, manipulaciji fajlovima i tokovima podataka, sa React dashboard-om u realnom vremenu.

![.NET Framework](https://img.shields.io/badge/.NET_Framework-4.7.2-purple)
![WCF](https://img.shields.io/badge/WCF-WebHttpBinding-blue)
![React](https://img.shields.io/badge/React-18-61DAFB)
![Leaflet](https://img.shields.io/badge/Leaflet-1.9-green)

---

<img width="800" height="450" alt="ezgif-6ac44770a6618e96" src="https://github.com/user-attachments/assets/b79503d2-592b-425a-8112-c97ef031740e" />



## Pregled sistema

Projekat simulira monitoring pametne elektroenergetske mreze Srbije sa 4 node-a: jednom elektranom (TE Nikola Tesla, Obrenovac) i tri potrosacka grada (Beograd, Novi Sad, Nis). Sistem ucitava dataset od 1000 vremenskih merenja, detektuje anomalije u frekvenciji i snazi, korelira vremenske uslove sa potrosnjom, i sve prikazuje na interaktivnom dashboardu sa Leaflet mapom.

### Arhitektura

```
smartgrid-dashboard (React)          Client (C# konzola)
        |                                    |
        |  HTTP/JSON polling (3s)            |  HTTP/JSON pozivi
        |                                    |
        +----------------+------------------+
                         |
                   WCF Service (port 5000)
                   /          |           \
          GridAnalysis    Weather      CORS
          Service         Service      Inspector
                   \          |           /
                    Core (analiza, eventi)
                         |
                    Data (CSV, streams, fajlovi)
                         |
                    Dataset (CSV fajl)
```

### Mrezna topologija

```
                 [Node 1 - TE Nikola Tesla]
                      Obrenovac
                     PROIZVODJAC
                    /      |      \
                   /       |       \
            [Node 2]   [Node 3]   [Node 4]
            Beograd    Novi Sad      Nis
           POTROSAC   POTROSAC   POTROSAC
            x1.1       x0.85      x0.7
```

Pocinju se od jednog dataseta merenja, a mnoziteljima (1.1, 0.85, 0.7) i random noise-om simuliramo razlicitu potrosnju po gradu.

---

## Struktura projekta

```
SmartGrid/
|-- Contracts/                  WCF ugovori i Data Transfer objekti
|   |-- Interfaces/
|   |   |-- IGridAnalysisService.cs    [ServiceContract] - grid operacije
|   |   |-- IWeatherService.cs         [ServiceContract] - vremenski podaci
|   |-- DTOs/
|       |-- GridReadingDto.cs          Jedno merenje iz dataseta
|       |-- AnomalyResultDto.cs        Detektovana anomalija
|       |-- StabilityReportDto.cs      Kompletan izvestaj
|       |-- NodeStatusDto.cs           Status jednog node-a
|       |-- WeatherDataDto.cs          Vremenski podaci iz API-ja
|       |-- WeatherCorrelationDto.cs   Korelacija vreme-potrosnja
|       |-- ConsumptionForecastDto.cs  SMA predikcija
|       |-- NotificationDto.cs         Event notifikacija
|
|-- Core/                       Poslovna logika (WCF-agnosticna)
|   |-- Analysis/
|   |   |-- FrequencyChangeDetector.cs   delta_f detekcija + Z-score
|   |   |-- PowerOverloadDetector.cs     P(t)=V*I detekcija + weather + Z-score
|   |   |-- ConsumptionPredictor.cs      Simple Moving Average forecast
|   |-- Events/
|   |   |-- GridEventManager.cs          Centralni event hub
|   |   |-- AnomalyEventArgs.cs          Custom EventArgs
|   |   |-- ThresholdEventArgs.cs        Custom EventArgs
|   |   |-- FaultEventArgs.cs            Custom EventArgs
|   |-- ExternalApi/
|       |-- OpenWeatherClient.cs         OpenWeatherMap API klijent
|
|-- Data/                       File I/O, Dispose pattern, Streams
|   |-- Readers/
|   |   |-- CsvDataReader.cs            IDisposable CSV parser
|   |-- Repository/
|   |   |-- GridDataRepository.cs       Centralno skladiste podataka
|   |-- Watchers/
|   |   |-- GridFileWatcher.cs          FileSystemWatcher + custom event
|   |-- Streams/
|       |-- DataCompressor.cs           GZipStream kompresija
|       |-- StreamTransfer.cs           MemoryStream, BufferedStream operacije
|
|-- Service/                    WCF host
|   |-- GridAnalysisService.cs         Implementacija IGridAnalysisService
|   |-- WeatherService.cs             Implementacija IWeatherService
|   |-- CorsMessageInspector.cs       CORS podrska za React
|   |-- Program.cs                    Entry point, inicijalizacija
|   |-- App.config                    WCF konfiguracija
|
|-- Client/                     C# konzolni test klijent
|   |-- Program.cs                    Interaktivni meni za testiranje
|
|-- smartgrid-dashboard/        React frontend
|   |-- src/
|   |   |-- App.js                    Kompletan dashboard
|   |-- public/
|       |-- index.html                Custom scrollbar stilovi
|
|-- Dataset/
    |-- Input/                  smart_grid_dataset.csv
    |-- Output/                 Rezultati analize
    |-- Archive/                Arhivirani fajlovi
```

---

## Dataset

Izvor: [Kaggle - Smart Grid Monitoring Dataset](https://www.kaggle.com/datasets/ziya07/smart-grid-monitoring-dataset)

1000 vremenskih merenja sa intervalom od 1 minuta (2024-01-01 00:00 do 16:39).

| Kolona | Opseg | Opis |
|--------|-------|------|
| Timestamp | 2024-01-01 00:00 - 16:39 | Vremenski pecat (svaki minut) |
| Voltage (V) | 199.7 - 260.2 | Napon u voltima |
| Current (A) | 7.6 - 22.4 | Struja u amperima |
| Power Usage (kW) | 1.86 - 5.28 | Potrosnja u kilovatima |
| Frequency (Hz) | 48.47 - 51.68 | Frekvencija mreze |
| Fault Indicator | 0, 1, 2 | 0=normalno, 1=kvar, 2=upozorenje |
| FFT_1 ... FFT_128 | razlicito | Fourier transform koeficijenti |

Distribucija kvarova: 340 normalnih (0), 349 kvarova (1), 311 upozorenja (2).

---

## Kljucni algoritmi

### 1. Detekcija naglih promena frekvencije

Iz specifikacije projekta:

```
delta_f = f(t) - f(t - delta_t)

Ako je |delta_f| > F_threshold --> podici dogadjaj
```

Implementacija u `FrequencyChangeDetector.Detect()` poredi frekvenciju svakog merenja sa prethodnim. Default prag je 1.0 Hz. Sa ovim pragom detektuje se 176 anomalija u datasetu.

Severity se odredjuje na osnovu magnitude:
- |delta_f| > prag * 3.0 --> Critical
- |delta_f| > prag * 2.0 --> High
- |delta_f| > prag * 1.5 --> Medium
- ostalo --> Low

### 2. Detekcija preopterecenja snage

Iz specifikacije projekta:

```
P(t) = V(t) * I(t)

Ako je P(t) > P_max_threshold --> podici dogadjaj
```

Implementacija u `PowerOverloadDetector.Detect()` racuna proizvod napona i struje za svako merenje. Default prag je 4000W. Sa ovim pragom detektuje se 117 preopterecenja.

#### Weather korelacija na prag

Temperatura utice na potrosnju elektricne energije. Kad je hladno, radi grejanje. Kad je vruce, radi klima. Oba povecavaju rizik preopterecenja.

```
Komforna zona: 15°C - 25°C (risk = 1.0)

Ako je T < 15°C:  risk = 1.0 + (15 - T) * 0.03
Ako je T > 25°C:  risk = 1.0 + (T - 25) * 0.03
Ako je vetar > 15 m/s:  risk += 0.1

Korigovani prag = bazni_prag * (1.0 - (risk - 1.0) * 0.3)
```

Primer: Na 35°C, risk factor = 1.3, prag se smanjuje sa 4000W na 3600W - sistem postaje osetljiviji.

### 3. Z-Score anomaly detection

Dodatna statisticka detekcija na voltage, current, power i frequency:

```
Z = (vrednost - prosek) / standardna_devijacija

Standardna devijacija = sqrt(sum((xi - mean)^2) / N)

Ako je |Z| > prag --> anomalija (default prag: 2.0)
```

Z-score detektuje outliere - merenja koja su statisticki neuobicajena u poredjenju sa ostatkom dataseta. Severity raste sa velicinom Z-score-a.

### 4. Predikcija potrosnje (SMA)

`ConsumptionPredictor` koristi Simple Moving Average sa prozorskom funkcijom od 50 merenja:

```
SMA = prosek poslednjih 50 merenja
Trend = (SMA_trenutni - SMA_prethodni) / velicina_prozora
Forecast[i] = SMA + trend * i
Confidence[i] = max(0.3, 1.0 - i * 0.03)
```

---

## Tehnologije i koncepti

### WCF (Windows Communication Foundation)

Servisno-orijentisana arhitektura sa dva servisa:

**GridAnalysisService** na `http://localhost:5000/api/grid`:
| Endpoint | Opis |
|----------|------|
| GET /readings?page=1&pageSize=50 | Paginacija merenja |
| GET /readings/{id} | Pojedinacno merenje |
| GET /report | Kompletan izvestaj |
| GET /anomalies/frequency?threshold=1.0 | delta_f anomalije |
| GET /anomalies/power?threshold=4000 | P(t)=V*I preopterecenja |
| GET /anomalies/zscore?threshold=2.0 | Z-score outlieri |
| GET /nodes | Status svih node-ova |
| GET /nodes/{nodeId} | Status jednog node-a |
| GET /forecast/{nodeId}?points=20 | SMA predikcija |
| GET /notifications?count=50 | Event notifikacije |
| GET /export | GZip kompresovani podaci |

**WeatherService** na `http://localhost:5000/api/weather`:
| Endpoint | Opis |
|----------|------|
| GET /current/{city} | Vreme za grad |
| GET /all | Vreme za sva 4 grada |
| GET /correlation | Weather-power korelacija |

Konfiguracija koristi `WebHttpBinding` sa `WebMessageFormat.Json` za REST/JSON komunikaciju. WCF hostovanje preko `WebServiceHost` u konzolnoj aplikaciji.

### Dispose pattern

Potpuna implementacija na `CsvDataReader`, `GridDataRepository`, `GridFileWatcher`, `DataCompressor`, `StreamTransfer`:

```csharp
public class CsvDataReader : IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            // managed resursi
            _reader?.Dispose();
            _bufferedStream?.Dispose();
            _fileStream?.Dispose();
        }
        _disposed = true;
    }

    ~CsvDataReader() { Dispose(false); }
}
```

### Fajlovi i direktorijumi

- `CsvDataReader` ucitava dataset iz `Dataset/Input/`
- `GridFileWatcher` prati folder za nove CSV fajlove (FileSystemWatcher)
- `StreamTransfer.ArchiveFile()` premesta obradjene fajlove u `Dataset/Archive/`
- Direktorijumi se automatski kreiraju ako ne postoje

### Tokovi podataka (Streams)

| Stream | Upotreba |
|--------|----------|
| FileStream | Citanje CSV fajla sa diska |
| BufferedStream | Bufferovano citanje za performanse (8KB buffer) |
| StreamReader | Tekstualni stream za parsiranje linija |
| MemoryStream | Buffer za JSON serijalizaciju |
| GZipStream | Kompresija podataka za mrezni prenos |

Stream chain u CsvDataReader:
```
FileStream --> BufferedStream (8KB) --> StreamReader --> ReadLine()
```

GZip kompresija u DataCompressor:
```
List<GridReadingDto> --> JSON --> byte[] --> GZipStream --> kompresovani byte[]
```

### Delegati i eventi

Custom delegati i eventi u `GridEventManager`:

```csharp
// Custom delegati
public delegate void AnomalyDetectedHandler(object sender, AnomalyEventArgs e);
public delegate void ThresholdExceededHandler(object sender, ThresholdEventArgs e);
public delegate void FaultDetectedHandler(object sender, FaultEventArgs e);

// Eventi
public event AnomalyDetectedHandler AnomalyDetected;
public event ThresholdExceededHandler ThresholdExceeded;
public event FaultDetectedHandler FaultDetected;
```

Tok dogadjaja:
```
FrequencyChangeDetector detektuje |delta_f| > prag
    --> eventManager.RaiseAnomalyDetected(...)
        --> GridEventManager.OnAnomalyDetected handler
            --> Kreira NotificationDto
            --> Cuva u listi notifikacija
                --> React polluje GET /notifications
                    --> Prikazuje u Live Feed
```

### Eksterni API

**OpenWeatherMap** (besplatan, zahteva API kljuc):
- Real-time vremenski podaci za Obrenovac, Beograd, Novi Sad, Nis
- Temperatura, vlaznost, vetar, opis uslova
- Koristi se za weather korelaciju sa potrosnjom
- .NET 4.7.2 koristi `WebClient` za HTTP pozive

### React Dashboard

Komponente:
| Komponenta | Funkcija |
|------------|----------|
| GridMap | Leaflet mapa Srbije sa node-ovima i linijama |
| StatCard | Prikaz prosecnih vrednosti (6 kartica) |
| NodeDetail | Detalji selektovanog node-a + vreme |
| AnomalyTable | Tabela anomalija sa filterima po tipu |
| WeatherCorrelation | Risk factor po gradu |
| NotificationFeed | Live feed dogadjaja |

Polling na 3 sekunde simulira real-time monitoring. Svaki poll pomera kursor u datasetu (circular buffer) pa se podaci menjaju.

---

## Pokretanje

### Preduslovi

- Visual Studio 2019+ sa .NET Framework 4.7.2
- Node.js 16+
- OpenWeatherMap API kljuc (besplatan: https://openweathermap.org/users/sign_up)

### 1. WCF Service

1. Otvoriti `SmartGrid.sln` u Visual Studio
2. U `Service/App.config` postaviti API kljuc:
   ```xml
   <add key="OpenWeatherApiKey" value="VAS_API_KLJUC" />
   ```
3. Staviti `smart_grid_dataset.csv` u `Dataset/Input/`
4. Registrovati HTTP URL-ove (jednom, kao Administrator):
   ```cmd
   netsh http add urlacl url=http://+:5000/api/grid/ user=Everyone
   netsh http add urlacl url=http://+:5000/api/weather/ user=Everyone
   ```
5. Set Service kao Startup Project i pokrenuti (F5)

### 2. React Dashboard

```cmd
cd smartgrid-dashboard
npm install
npm start
```

Otvara se na `http://localhost:3000`. Proxy u `package.json` prosledjuje pozive ka WCF-u na portu 5000.

### 3. Console Client

Right click Client --> Debug --> Start new instance (dok Service radi).

---

## Reference

- [Kaggle Smart Grid Monitoring Dataset](https://www.kaggle.com/datasets/ziya07/smart-grid-monitoring-dataset)
- [OpenWeatherMap API](https://openweathermap.org/api)
- [WCF WebHttpBinding](https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/wcf-web-http-programming-model)
- [Leaflet.js](https://leafletjs.com/)
