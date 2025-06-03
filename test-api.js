// API endpoint testleri için basit bir script
const axios = require('axios');

// API temel URL'si
const API_BASE_URL = 'http://localhost:5112/api';

// Örnek token (gerçek bir token değil)
const TOKEN = 'YOUR_JWT_TOKEN_HERE';

// Test edilecek endpoint'ler
const endpoints = [
  { 
    name: 'Klasör Oluşturma', 
    method: 'post', 
    url: '/folder/create', 
    data: { Name: 'Test Klasör', ParentFolderId: null } 
  },
  { 
    name: 'Klasör Paylaşımı', 
    method: 'post', 
    url: '/folder/FOLDER_ID_HERE/share', 
    data: { 
      SharedWithUserId: 'USER_ID_HERE', 
      Permission: 1, 
      ExpiresAt: null, 
      ShareNote: 'Test paylaşımı' 
    } 
  },
  { 
    name: 'Dosya Paylaşımı', 
    method: 'post', 
    url: '/file/share-multiple', 
    data: { 
      FileId: 'FILE_ID_HERE', 
      UserEmails: ['test@example.com'] 
    } 
  }
];

// Her bir endpoint için log bilgisini göster
endpoints.forEach(endpoint => {
  const fullUrl = `${API_BASE_URL}${endpoint.url}`;
  console.log(`${endpoint.name}:`);
  console.log(`  Metod: ${endpoint.method.toUpperCase()}`);
  console.log(`  URL: ${fullUrl}`);
  console.log(`  Veri: ${JSON.stringify(endpoint.data)}`);
  console.log('-------------------');
});

// NOT: Bu script gerçek API çağrıları yapmaz, sadece URL'leri ve verileri gösterir
// Gerçek çağrılar için aşağıdaki kod kullanılabilir:
/* 
async function testEndpoint(endpoint) {
  try {
    const response = await axios({
      method: endpoint.method,
      url: `${API_BASE_URL}${endpoint.url}`,
      data: endpoint.data,
      headers: {
        'Authorization': `Bearer ${TOKEN}`,
        'Content-Type': 'application/json'
      }
    });
    console.log(`${endpoint.name} - Başarılı:`, response.status);
    console.log(response.data);
  } catch (error) {
    console.error(`${endpoint.name} - Hata:`, error.response?.status);
    console.error(error.response?.data || error.message);
  }
}

async function runTests() {
  for (const endpoint of endpoints) {
    console.log(`Testing: ${endpoint.name}`);
    await testEndpoint(endpoint);
    console.log('-------------------');
  }
}

runTests();
*/
