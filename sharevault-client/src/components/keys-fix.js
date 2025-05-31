// Bu dosya, React key uyarılarını çözmek için rehber olarak kullanılabilir
// React key uyarılarını çözmek için aşağıdaki yönergeleri izleyin:

/*
1. Tablo bileşenlerinde key sorunları:
   - Her render fonksiyonu içinde birden fazla JSX öğesi döndürülüyorsa, her birine benzersiz key eklenmeli
   - Space, Row, Col, Tooltip gibi sarmalayıcı bileşenlere key ekleyin
   
2. Dropdown menülerinde:
   - Dropdown bileşenlerine key ekleyin (key={`dropdown-${record.id}`})
   - Menu itemlerine zaten key ekli, ancak items dizisi içinde map ile üretilen itemlere key eklenmeli
   
3. Table bileşenlerinde:
   - dataSource için rowKey prop eklenmiş olduğundan emin olun
   - columns içindeki render fonksiyonlarında birden fazla öğe döndürülüyorsa, öğelere key ekleyin
   
4. Modal ve Form içindeki listelere:
   - Modal ve form içinde oluşturulan listelere key ekleyin
   - Özellikle map ile üretilen tüm öğelere key ekleyin
*/

// Örnek düzeltmeler:

// 1. Space bileşenine key ekleme:
// ÖNCESİ:
// render: (_: any, record: ExtendedFileDto) => (
//   <Space>
//     <Button>İşlem 1</Button>
//     <Button>İşlem 2</Button>
//   </Space>
// )

// SONRASI:
// render: (_: any, record: ExtendedFileDto) => (
//   <Space key={`actions-${record.id}`}>
//     <Button key={`btn1-${record.id}`}>İşlem 1</Button>
//     <Button key={`btn2-${record.id}`}>İşlem 2</Button>
//   </Space>
// )

// 2. Dropdown'a key ekleme:
// ÖNCESİ:
// <Dropdown menu={{ items: [...] }}>
//   <Button icon={<MoreOutlined />} />
// </Dropdown>

// SONRASI:
// <Dropdown key={`dropdown-${record.id}`} menu={{ items: [...] }}>
//   <Button icon={<MoreOutlined />} />
// </Dropdown>

// 3. Modal içindeki liste öğelerine key ekleme:
// ÖNCESİ:
// {items.map(item => (
//   <div>{item.name}</div>
// ))}

// SONRASI:
// {items.map(item => (
//   <div key={item.id}>{item.name}</div>
// ))}
