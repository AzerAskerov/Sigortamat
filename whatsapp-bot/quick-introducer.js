const { Client, LocalAuth } = require('whatsapp-web.js');

class QuickIntroducer {
    constructor() {
        this.client = new Client({
            authStrategy: new LocalAuth({
                name: 'sigorta-session',
                dataPath: './auth_data'
            }),
            puppeteer: {
                headless: false, // Browser göstəririk tanıtma üçün
                args: [
                    '--no-sandbox',
                    '--disable-setuid-sandbox'
                ]
            }
        });
    }

    async introduceContact(phoneNumber) {
        try {
            console.log('🚀 WhatsApp Quick Introducer başladılır...');
            
            await this.client.initialize();
            
            // Hazır olana qədər gözləyirik
            while (!this.client.info) {
                await new Promise(resolve => setTimeout(resolve, 1000));
            }
            
            console.log('✅ WhatsApp Client hazırdır!');
            
            // Nömrəni formatlayırıq
            const formattedNumber = this.formatPhoneNumber(phoneNumber);
            const chatId = `${formattedNumber}@c.us`;
            
            console.log(`🔍 Kontakt tanıdılır: ${formattedNumber}`);
            
            // Kontaktı yoxlayırıq və mesaj göndəririk
            const contact = await this.client.getContactById(chatId);
            console.log(`👤 Kontakt: ${contact.name || contact.number}`);
            
            // Kiçik test mesajı göndəririk
            await this.client.sendMessage(chatId, '🔄 Kontakt tanıdıldı - Smart WhatsApp sistemi');
            console.log('✅ Tanıtma mesajı göndərildi!');
            
            // 5 saniyə gözləyirik
            await new Promise(resolve => setTimeout(resolve, 5000));
            
            await this.client.destroy();
            console.log('✅ Tanıtma tamamlandı!');
            
            return true;
        } catch (error) {
            console.error('❌ Tanıtma xətası:', error.message);
            try {
                await this.client.destroy();
            } catch (e) {
                // Ignore cleanup errors
            }
            return false;
        }
    }

    formatPhoneNumber(phoneNumber) {
        let cleaned = phoneNumber.replace(/\D/g, '');
        if (!cleaned.startsWith('994')) {
            if (cleaned.startsWith('0')) {
                cleaned = '994' + cleaned.substring(1);
            } else {
                cleaned = '994' + cleaned;
            }
        }
        return cleaned;
    }
}

// Komanda sətrindən istifadə
async function main() {
    const phoneNumber = process.argv[2];
    
    if (!phoneNumber) {
        console.error('❌ Telefon nömrəsi tələb olunur!');
        process.exit(1);
    }
    
    const introducer = new QuickIntroducer();
    const success = await introducer.introduceContact(phoneNumber);
    
    process.exit(success ? 0 : 1);
}

if (require.main === module) {
    main().catch(console.error);
}
