# SigortaYoxla + WhatsApp Runner
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " SigortaYoxla + WhatsApp Bulk Sender" -ForegroundColor Cyan  
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1] Full pipeline (Sigorta yoxla + WhatsApp gonder)" -ForegroundColor Green
Write-Host "[2] Yalniz sigorta yoxla (WhatsApp olmadan)" -ForegroundColor Yellow
Write-Host "[3] Yalniz WhatsApp test" -ForegroundColor Blue
Write-Host "[4] WhatsApp QR setup (ilk defe)" -ForegroundColor Magenta
Write-Host "[5] WhatsApp bulk mesaj gonder (JSON-dan)" -ForegroundColor Blue
Write-Host "[6] Cixis" -ForegroundColor Red
Write-Host ""

$choice = Read-Host "Seciminizi edin (1-6)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "🚀 Full pipeline başlayır..." -ForegroundColor Green
        Write-Host ""
        & dotnet run
    }
    
    "2" {
        Write-Host ""
        Write-Host "🔍 Yalnız sığorta yoxlanır..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Qeyd: Program.cs-də WhatsApp hissəsini deaktiv edin" -ForegroundColor Yellow
        Read-Host "Davam etmək üçün Enter basın"
    }
    
    "3" {
        Write-Host ""
        Write-Host "📱 WhatsApp test edilir..." -ForegroundColor Blue
        Write-Host ""
        Set-Location "whatsapp-bot"
        & node whatsapp-sender.js test
        Set-Location ".."
    }
    
    "4" {
        Write-Host ""
        Write-Host "📱 WhatsApp QR setup..." -ForegroundColor Magenta
        Write-Host "Telefonda WhatsApp Web açın və QR kodu skan edin" -ForegroundColor Yellow
        Write-Host ""
        Set-Location "whatsapp-bot"
        & node whatsapp-sender.js test
        Set-Location ".."
    }
    
    "5" {
        Write-Host ""
        Write-Host "📦 WhatsApp bulk mesaj göndərilir..." -ForegroundColor Blue
        Write-Host ""
        Set-Location "whatsapp-bot"
        & node whatsapp-sender.js bulk messages.json
        Set-Location ".."
    }
    
    "6" {
        Write-Host ""
        Write-Host "👋 Görüşənədək!" -ForegroundColor Green
        exit
    }
    
    default {
        Write-Host ""
        Write-Host "❌ Yanlış seçim! Yenidən cəhd edin." -ForegroundColor Red
        Read-Host "Davam etmək üçün Enter basın"
    }
}

Write-Host ""
Write-Host "✅ İşlem tamamlandı!" -ForegroundColor Green
Read-Host "Çıxmaq üçün Enter basın"
