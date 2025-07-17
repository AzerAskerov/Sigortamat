@echo off
echo ============================================
echo  SigortaYoxla + WhatsApp Bulk Sender
echo ============================================
echo.

echo [1] Full pipeline (Sigorta yoxla + WhatsApp gonder)
echo [2] Yalniz sigorta yoxla (WhatsApp olmadan)
echo [3] Yalniz WhatsApp test
echo [4] WhatsApp QR setup (ilk defe)
echo [5] Cixis
echo.

set /p choice="Seciminizi edin (1-5): "

if "%choice%"=="1" (
    echo.
    echo 🚀 Full pipeline başlayır...
    echo.
    dotnet run
    goto end
)

if "%choice%"=="2" (
    echo.
    echo 🔍 Yalnız sığorta yoxlanır...
    echo.
    rem Program.cs-de WhatsApp hissesini comment-leyib tekrar build etmek lazimdir
    echo Qeyd: Program.cs-de WhatsApp hissesini deaktiv edin
    pause
    goto end
)

if "%choice%"=="3" (
    echo.
    echo 📱 WhatsApp test edilir...
    echo.
    cd whatsapp-bot
    node whatsapp-sender.js test
    cd ..
    goto end
)

if "%choice%"=="4" (
    echo.
    echo 📱 WhatsApp QR setup...
    echo Telefonda WhatsApp Web açın və QR kodu skan edin
    echo.
    cd whatsapp-bot
    node whatsapp-sender.js test
    cd ..
    goto end
)

if "%choice%"=="5" (
    echo.
    echo 👋 Görüşənədək!
    goto end
)

echo.
echo ❌ Yanlış seçim! Yenidən cəhd edin.
pause
goto start

:end
echo.
echo ✅ İşlem tamamlandı!
pause
