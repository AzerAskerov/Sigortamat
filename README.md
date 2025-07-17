# SigortaYoxla - Avtomobil Sığorta Yoxlayıcısı + WhatsApp Bot

Bu layihə avtomobil nömrələrinə görə sığorta məlumatlarını yoxlayır və nəticələri WhatsApp vasitəsilə göndərir.

## 🚀 Xüsusiyyətlər

- ✅ Bulk avtomobil nömrəsi yoxlaması (Selenium ilə)
- 📱 WhatsApp vasitəsilə avtomatik mesaj göndərmə
- 🔄 Queue və təkrarlanan işlər üçün hazır
- 📊 Detailed reporting və loglar

## 📋 Tələblər

### C# Hissəsi
- .NET 9.0+
- Selenium WebDriver
- Chrome browser

### WhatsApp Bot Hissəsi  
- Node.js 16+
- npm

## ⚙️ Quraşdırma

### 1. C# Proyektini hazırlayın
```bash
cd sigortaYoxla
dotnet restore
dotnet build
```

### 2. WhatsApp Bot-u quraşdırın
```bash
cd whatsapp-bot
npm install
```

### 3. WhatsApp-ı fəallaşdırın
İlk dəfə işlədərkən QR kod ilə WhatsApp-ınızı qoşmalısınız:

```bash
cd whatsapp-bot
node whatsapp-sender.js test
```

QR kodu skan edin və "WhatsApp Client hazırdır!" mesajını gözləyin.

## 🎯 İstifadə

### Əsas C# Proqramı (Full Pipeline)
```bash
dotnet run
```

Bu komanda:
1. Avtomobil nömrələrini yoxlayır
2. Nəticələri formatlayır  
3. WhatsApp mesajları göndərir

### Ayrı-ayrılıqda WhatsApp İstifadəsi

#### Tək mesaj göndər:
```bash
cd whatsapp-bot
node whatsapp-sender.js send 0501234567 "Test mesajı"
```

#### Bulk mesajlar göndər:
```bash
cd whatsapp-bot  
node whatsapp-sender.js bulk messages.json
```

#### Test mesajı:
```bash
cd whatsapp-bot
node whatsapp-sender.js test
```

## 📁 Fayl Strukturu

```
sigortaYoxla/
├── Program.cs              # Əsas C# proqramı
├── SigortaChecker.cs       # Selenium sığorta yoxlayıcısı
├── WhatsAppService.cs      # C# WhatsApp xidməti
├── whatsapp-bot/
│   ├── package.json        # Node.js dependencies
│   ├── whatsapp-sender.js  # WhatsApp bot
│   ├── messages.json       # Bulk mesaj nümunəsi
│   └── auth_data/          # WhatsApp session məlumatları
└── README.md
```

## 🔧 Konfiqurasiya

### Telefon Nömrəsi Mapping-i
`Program.cs` faylında `GetPhoneNumberForCar` funksiyasını redaktə edin:

```csharp
private static string GetPhoneNumberForCar(string carNumber)
{
    var phoneMapping = new Dictionary<string, string>
    {
        { "90HB986", "0501234567" },  // Öz telefon nömrənizi yazın
        { "90HB987", "0559876543" },
        // Daha çox mapping əlavə edin...
    };
    
    return phoneMapping.TryGetValue(carNumber, out var phoneNumber) ? phoneNumber : "";
}
```

### WhatsApp Mesaj Formatı
`WhatsAppService.cs`-də `FormatInsuranceMessage` funksiyasını öz ehtiyacınıza görə redaktə edin.

## 📱 WhatsApp Bot Xüsusiyyətləri

- **QR Authentication**: İlk dəfə QR kod skan edin
- **Session Saxlanması**: Növbəti dəfələr avtomatik qoşulur
- **Bulk Messaging**: Çox mesaj paralel göndərə bilir
- **Error Handling**: Uğursuz mesajları qeyd edir
- **Rate Limiting**: Mesajlar arası 2 saniyə gözləmə

## 🔄 Queue İşləri üçün İstifadə

Bu sistem təkrarlanan işlər üçün hazır hazırlanmışdır:

1. **Scheduled Jobs**: Cron job və ya Windows Task Scheduler ilə
2. **Message Queue**: RabbitMQ, Azure Service Bus və s. ilə inteqrasiya
3. **Database Integration**: Avtomobil-telefon mapping-i üçün

## 🛠️ Problemlərin Həlli

### WhatsApp QR Kod Göstərmir
```bash
cd whatsapp-bot
rm -rf auth_data
node whatsapp-sender.js test
```

### C# Build Error
```bash
dotnet clean
dotnet restore  
dotnet build
```

### Node.js Package Issues
```bash
cd whatsapp-bot
rm -rf node_modules
npm install
```

## 📞 Dəstək

Hər hansı problem olduqda issue açın və ya pull request göndərin.

## ⚠️ Qeydlər

- Test rejimində öz telefon nömrənizi istifadə edin
- WhatsApp Business API qaydalarına riayət edin  
- Rate limiting-ə diqqət edin (spam kimi qəbul edilə bilər)
- Auth məlumatlarını (.auth_data/) git-ə commit etməyin
