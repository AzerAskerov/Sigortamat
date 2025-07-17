const fs = require('fs');
const path = require('path');

class WhatsAppCacheCleaner {
    constructor() {
        this.authDataPath = './auth_data';
    }

    async clearCache() {
        try {
            console.log('🧹 WhatsApp cache təmizlənir...');
            
            // Session folder-ni silirik
            const sessionPath = path.join(this.authDataPath, 'session');
            if (fs.existsSync(sessionPath)) {
                console.log('📁 Session folder silinir...');
                fs.rmSync(sessionPath, { recursive: true, force: true });
                console.log('✅ Session folder silindi');
            }
            
            // User data cache-ni də silirik
            const userDataPaths = [
                path.join(this.authDataPath, 'session', 'Default'),
                path.join(this.authDataPath, 'session', 'Profile 1'),
                path.join(this.authDataPath, 'RemoteWebAppsData')
            ];
            
            for (const userPath of userDataPaths) {
                if (fs.existsSync(userPath)) {
                    console.log(`🗑️ Cache silinir: ${userPath}`);
                    fs.rmSync(userPath, { recursive: true, force: true });
                }
            }
            
            console.log('✅ WhatsApp cache tamamilə təmizləndi!');
            console.log('🔄 İndi yeni sessiya üçün debug tool işlədin:');
            console.log('   node debug-whatsapp.js [nömrə] [mesaj]');
            
            return true;
        } catch (error) {
            console.error('❌ Cache təmizləmə xətası:', error.message);
            return false;
        }
    }
}

// Komanda sətrindən istifadə
async function main() {
    const cleaner = new WhatsAppCacheCleaner();
    await cleaner.clearCache();
}

if (require.main === module) {
    main().catch(console.error);
}

module.exports = WhatsAppCacheCleaner;
