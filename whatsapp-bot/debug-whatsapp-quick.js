const { Client, LocalAuth } = require('whatsapp-web.js');

// Komanda sətrindən nömrəni alırıq
const phoneNumber = process.argv[2];

if (!phoneNumber) {
    console.error('❌ Telefon nömrəsi verilmədi!');
    process.exit(1);
}

async function quickIntroduce() {
    const client = new Client({
        authStrategy: new LocalAuth({
            name: 'sigorta-session',
            dataPath: './auth_data'
        }),
        puppeteer: {
            headless: false, // Browser açırıq ki, kontakt yüklənsin
            args: [
                '--no-sandbox',
                '--disable-setuid-sandbox'
            ]
        }
    });

    try {
        console.log(`🔍 Debug: ${phoneNumber} tanıdılır...`);
        
        client.on('ready', async () => {
            console.log('✅ WhatsApp hazırdır');
            
            try {
                // Nömrəni formatlayırıq
                let formattedNumber = phoneNumber.replace(/\D/g, '');
                if (!formattedNumber.startsWith('994')) {
                    if (formattedNumber.startsWith('0')) {
                        formattedNumber = '994' + formattedNumber.substring(1);
                    } else {
                        formattedNumber = '994' + formattedNumber;
                    }
                }
                
                const chatId = `${formattedNumber}@c.us`;
                
                // Kontaktı yoxlayırıq
                const contact = await client.getContactById(chatId);
                console.log(`👤 Kontakt: ${contact.name || contact.number}`);
                
                // Kiçik test mesajı göndəririk
                await client.sendMessage(chatId, 'Debug tanıtma mesajı ✅');
                console.log('✅ Debug mesajı göndərildi');
                
                // 3 saniyə gözləyirik, sonra bağlanırıq
                setTimeout(async () => {
                    await client.destroy();
                    console.log('✅ Debug tamamlandı');
                    process.exit(0);
                }, 3000);
                
            } catch (error) {
                console.error('❌ Kontakt xətası:', error.message);
                await client.destroy();
                process.exit(1);
            }
        });

        client.on('auth_failure', () => {
            console.error('❌ Autentifikasiya uğursuz');
            process.exit(1);
        });

        await client.initialize();
        
    } catch (error) {
        console.error('❌ Debug xətası:', error.message);
        process.exit(1);
    }
}

quickIntroduce();
