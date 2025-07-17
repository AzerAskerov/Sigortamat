const { Client, LocalAuth } = require('whatsapp-web.js');
const qrcode = require('qrcode-terminal');

class WhatsAppSender {
    constructor() {
        this.client = new Client({
            authStrategy: new LocalAuth({
                name: 'sigorta-session',
                dataPath: './auth_data'
            }),
            puppeteer: {
                headless: "new", // Yeni headless modu - daha sabit
                args: [
                    '--no-sandbox',
                    '--disable-setuid-sandbox',
                    '--disable-dev-shm-usage',
                    '--disable-accelerated-2d-canvas',
                    '--no-first-run',
                    '--disable-gpu',
                    '--disable-background-timer-throttling',
                    '--disable-backgrounding-occluded-windows',
                    '--disable-renderer-backgrounding',
                    '--disable-features=TranslateUI',
                    '--disable-ipc-flooding-protection'
                ]
            },
            // Timeout əlavə edirik
            qrTimeoutMs: 60000, // 60 saniyə QR timeout
            authTimeoutMs: 60000, // 60 saniyə auth timeout  
            restartOnAuthFail: true
        });

        this.isReady = false;
        this.setupEventHandlers();
    }

    setupEventHandlers() {
        // QR kod göstər - KRİTİK: headless modda QR gəlməməlidir!
        this.client.on('qr', (qr) => {
            console.error('❌ SESSIYA BİTİB! QR kod tələb olunur!');
            console.error('🔄 Debug tool ilə yenidən login olun:');
            console.error('   node debug-whatsapp.js [nömrə] [mesaj]');
            // Prosesi bitiririk - QR gözləməyəcəyik
            process.exit(1);
        });

        // Hazır vəziyyət
        this.client.on('ready', () => {
            console.log('✅ WhatsApp Client hazırdır!');
            this.isReady = true;
        });

        // Autentifikasiya uğurlu
        this.client.on('authenticated', () => {
            console.log('🔐 Autentifikasiya uğurludur!');
        });

        // Autentifikasiya uğursuz
        this.client.on('auth_failure', (msg) => {
            console.error('❌ Autentifikasiya uğursuz:', msg);
            console.error('🔄 Sessiya bitib, debug tool işlədin!');
            process.exit(1);
        });

        // Disconnect
        this.client.on('disconnected', (reason) => {
            console.log('🔌 WhatsApp disconnect oldu:', reason);
            this.isReady = false;
        });

        // Loading screen event
        this.client.on('loading_screen', (percent, message) => {
            console.log(`⏳ Yüklənir: ${percent}% - ${message}`);
        });
    }

    async initialize() {
        console.log('🚀 WhatsApp Client başladılır...');
        
        // Əvvəlcə session yoxlayırıq
        const sessionExists = await this.checkSessionExists();
        if (!sessionExists) {
            console.error('❌ WhatsApp sessiyası tapılmadı!');
            console.error('🔄 Əvvəlcə debug tool ilə login olun:');
            console.error('   node debug-whatsapp.js [nömrə] [mesaj]');
            throw new Error('Session tapılmadı');
        }
        
        try {
            // Timeout ilə initialize
            const initPromise = this.client.initialize();
            const timeoutPromise = new Promise((_, reject) => {
                setTimeout(() => reject(new Error('Initialize timeout (30s)')), 30000);
            });
            
            await Promise.race([initPromise, timeoutPromise]);
            
            // Client hazır olana qədər gözləyirik
            let attempts = 0;
            while (!this.isReady && attempts < 15) {
                await new Promise(resolve => setTimeout(resolve, 1000));
                attempts++;
            }
            
            if (!this.isReady) {
                throw new Error('Client 15 saniyədə hazır olmadı - sessiya bitmiş ola bilər');
            }
            
            console.log('✅ WhatsApp Client tam hazırdır!');
        } catch (error) {
            console.error('❌ WhatsApp Client başlamadı:', error.message);
            if (error.message.includes('Session') || error.message.includes('QR')) {
                console.error('🔄 Debug tool ilə yenidən login olun!');
            }
            throw error;
        }
    }

    async checkSessionExists() {
        const fs = require('fs');
        const path = require('path');
        
        try {
            const sessionPath = path.join('./auth_data', 'session');
            return fs.existsSync(sessionPath);
        } catch (error) {
            return false;
        }
    }

    async sendMessage(phoneNumber, message) {
        if (!this.isReady) {
            throw new Error('WhatsApp Client hazır deyil!');
        }

        try {
            // Nömrəni formatlayırıq (994 prefix ilə)
            const formattedNumber = this.formatPhoneNumber(phoneNumber);
            const chatId = `${formattedNumber}@c.us`;

            console.log(`📤 Mesaj göndərilir: ${formattedNumber}`);
            
            // Kontaktı yoxlayırıq və cache-ə əlavə edirik
            try {
                const contact = await this.client.getContactById(chatId);
                console.log(`👤 Kontakt: ${contact.name || contact.number}`);
            } catch (error) {
                console.log(`⚠️ Kontakt yoxlanıla bilmədi, amma davam edirəm...`);
            }
            
            await this.client.sendMessage(chatId, message);
            console.log(`✅ Mesaj göndərildi: ${formattedNumber}`);
            
            return {
                success: true,
                phoneNumber: formattedNumber,
                message: message
            };
        } catch (error) {
            console.error(`❌ Mesaj göndərilmədi (${phoneNumber}):`, error.message);
            return {
                success: false,
                phoneNumber: phoneNumber,
                error: error.message
            };
        }
    }

    formatPhoneNumber(phoneNumber) {
        // Nömrədən yalnız rəqəmləri götürürük
        let cleaned = phoneNumber.replace(/\D/g, '');
        
        // Əgər 994 ilə başlamırsa, əlavə edirik
        if (!cleaned.startsWith('994')) {
            if (cleaned.startsWith('0')) {
                cleaned = '994' + cleaned.substring(1);
            } else {
                cleaned = '994' + cleaned;
            }
        }
        
        return cleaned;
    }

    async sendBulkMessages(recipients) {
        if (!this.isReady) {
            throw new Error('WhatsApp Client hazır deyil!');
        }

        const results = [];
        
        for (const recipient of recipients) {
            try {
                await new Promise(resolve => setTimeout(resolve, 2000)); // 2 saniyə gözləmə
                
                const result = await this.sendMessage(recipient.phoneNumber, recipient.message);
                results.push(result);
                
            } catch (error) {
                results.push({
                    success: false,
                    phoneNumber: recipient.phoneNumber,
                    error: error.message
                });
            }
        }
        
        return results;
    }

    async destroy() {
        if (this.client) {
            console.log('🔌 WhatsApp Client bağlanır...');
            await this.client.destroy();
        }
    }
}

// Komanda sətrindən istifadə
async function main() {
    const args = process.argv.slice(2);
    
    if (args.length === 0) {
        console.log(`
📱 WhatsApp Sender İstifadəsi:

Tək mesaj:
node whatsapp-sender.js send <nömrə> "<mesaj>"

Bulk mesaj (JSON fayldan):
node whatsapp-sender.js bulk <json-fayl-yolu>

Test:
node whatsapp-sender.js test

Nümunələr:
node whatsapp-sender.js send 0501234567 "Salam, test mesajı"
node whatsapp-sender.js bulk messages.json
        `);
        return;
    }

    const whatsApp = new WhatsAppSender();
    
    try {
        await whatsApp.initialize();
        
        // Hazır olana qədər gözləyirik
        while (!whatsApp.isReady) {
            await new Promise(resolve => setTimeout(resolve, 1000));
        }
        
        const command = args[0];
        
        switch (command) {
            case 'send':
                if (args.length < 3) {
                    console.error('❌ Nömrə və mesaj tələb olunur!');
                    return;
                }
                
                const phoneNumber = args[1];
                const message = args.slice(2).join(' ');
                
                const result = await whatsApp.sendMessage(phoneNumber, message);
                console.log('📋 Nəticə:', result);
                break;
                
            case 'bulk':
                if (args.length < 2) {
                    console.error('❌ JSON fayl yolu tələb olunur!');
                    return;
                }
                
                const fs = require('fs');
                const filePath = args[1];
                
                if (!fs.existsSync(filePath)) {
                    console.error('❌ Fayl tapılmadı:', filePath);
                    return;
                }
                
                const recipients = JSON.parse(fs.readFileSync(filePath, 'utf8'));
                console.log(`📦 ${recipients.length} mesaj göndəriləcək...`);
                
                const bulkResults = await whatsApp.sendBulkMessages(recipients);
                
                console.log('\n📊 BULK NƏTİCƏLƏR:');
                console.log('='.repeat(50));
                bulkResults.forEach((res, index) => {
                    console.log(`${index + 1}. ${res.phoneNumber}: ${res.success ? '✅' : '❌'}`);
                    if (!res.success) {
                        console.log(`   Səbəb: ${res.error}`);
                    }
                });
                break;
                
            case 'test':
                console.log('🧪 Test mesajı göndərilir...');
                const testResult = await whatsApp.sendMessage('0501234567', 'Test mesajı - SigortaYoxla sistemi');
                console.log('📋 Test nəticəsi:', testResult);
                break;
                
            default:
                console.error('❌ Naməlum komanda:', command);
        }
        
    } catch (error) {
        console.error('❌ Xəta baş verdi:', error);
    } finally {
        await whatsApp.destroy();
        process.exit(0);
    }
}

// Əgər bu fayl birbaşa çalışdırılırsa
if (require.main === module) {
    main().catch(console.error);
}

module.exports = WhatsAppSender;
