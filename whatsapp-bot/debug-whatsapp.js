const { Client, LocalAuth } = require('whatsapp-web.js');

class WhatsAppDebugger {
    constructor() {
        this.client = new Client({
            authStrategy: new LocalAuth({
                name: 'sigorta-session',
                dataPath: './auth_data'
            }),
            puppeteer: {
                headless: false, // Browser göstəririk debug üçün
                args: [
                    '--no-sandbox',
                    '--disable-setuid-sandbox'
                ]
            }
        });
        
        this.setupEventHandlers();
    }

    setupEventHandlers() {
        this.client.on('ready', () => {
            console.log('✅ WhatsApp Client hazırdır!');
        });

        this.client.on('message', msg => {
            console.log('📨 Mesaj alındı:', msg.from, ':', msg.body);
        });

        this.client.on('message_create', msg => {
            console.log('📤 Mesaj yaradıldı:', msg.to, ':', msg.body);
        });

        this.client.on('message_ack', (msg, ack) => {
            console.log('✅ Mesaj təsdiqi:', msg.to, 'Status:', ack);
            /*
            ACK Status:
            1 = Göndərildi (sent)
            2 = Çatdırıldı (delivered) 
            3 = Oxundu (read)
            */
        });
    }

    async initialize() {
        console.log('🚀 WhatsApp Debug Client başladılır...');
        await this.client.initialize();
    }

    async checkContact(phoneNumber) {
        try {
            const formattedNumber = this.formatPhoneNumber(phoneNumber);
            const contactId = `${formattedNumber}@c.us`;
            
            console.log(`🔍 Kontakt yoxlanır: ${formattedNumber}`);
            
            // Kontaktın mövcudluğunu yoxlayırıq
            const contact = await this.client.getContactById(contactId);
            console.log('📱 Kontakt məlumatları:', {
                name: contact.name,
                number: contact.number,
                isWAContact: contact.isWAContact,
                isBusiness: contact.isBusiness
            });
            
            return contact;
        } catch (error) {
            console.error('❌ Kontakt yoxlanıla bilmədi:', error.message);
            return null;
        }
    }

    async sendMessageWithDebug(phoneNumber, message) {
        try {
            const formattedNumber = this.formatPhoneNumber(phoneNumber);
            const chatId = `${formattedNumber}@c.us`;
            
            console.log(`📤 Debug mesaj göndərilir: ${formattedNumber}`);
            
            // Kontaktı yoxlayırıq
            await this.checkContact(phoneNumber);
            
            // Mesajı göndəririk
            const sentMessage = await this.client.sendMessage(chatId, message);
            console.log('✅ Mesaj göndərildi, ID:', sentMessage.id._serialized);
            
            return sentMessage;
        } catch (error) {
            console.error('❌ Debug mesaj xətası:', error);
            throw error;
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

    async destroy() {
        await this.client.destroy();
    }
}

// İstifadə nümunəsi
async function debugWhatsApp() {
    const phoneNumber = process.argv[2];
    const message = process.argv[3];
    
    if (!phoneNumber) {
        console.error('❌ Telefon nömrəsi tələb olunur!');
        console.log('💡 İstifadə: node debug-whatsapp.js NÖMRƏ "MESAJ"');
        process.exit(1);
    }
    
    if (!message) {
        console.error('❌ Mesaj tələb olunur!');
        console.log('💡 İstifadə: node debug-whatsapp.js NÖMRƏ "MESAJ"');
        process.exit(1);
    }
    
    const whatsappDebugger = new WhatsAppDebugger();
    
    try {
        await whatsappDebugger.initialize();
        
        // Hazır olana qədər gözləyirik
        while (!whatsappDebugger.client.info) {
            await new Promise(resolve => setTimeout(resolve, 1000));
        }
        
        console.log('📋 WhatsApp məlumatları:', whatsappDebugger.client.info);
        
        // Komandadan gələn mesajı göndəririk
        await whatsappDebugger.sendMessageWithDebug(phoneNumber, message);
        
        // 30 saniyə gözləyirik ki, response gəlsin
        console.log('⏳ 30 saniyə gözləyirik...');
        await new Promise(resolve => setTimeout(resolve, 30000));
        
    } catch (error) {
        console.error('❌ Debug xətası:', error);
    } finally {
        await whatsappDebugger.destroy();
    }
}

if (require.main === module) {
    debugWhatsApp().catch(console.error);
}
