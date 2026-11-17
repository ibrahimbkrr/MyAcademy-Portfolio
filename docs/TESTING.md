# Testleri çalıştırma

.NET 9 SDK ve browser testleri için Microsoft Edge gerekir. Release koşuları açık Debug uygulamasından bağımsızdır.

```powershell
dotnet restore MyAcademy_PortfolioProject.sln
dotnet build MyAcademy_PortfolioProject.sln -c Release
dotnet test MyAcademy_PortfolioProject.sln -c Release
```

Varsayılan koşuda 92 test çalışır; SQL Server opt-in yoksa 4, doğrulanmış restore kopyası yoksa 1 test SKIP olur. SKIP, PASS değildir. Bu repo için son tam yerel koşu 97/97 PASS'tir; bu sonuç gerekli ortamların sağlandığı koşuya aittir.

## Standart entegrasyon ve browser testleri

Her standart entegrasyon testi kendi bellek içi SQLite veritabanını kullanır. Testing ortamı Development seeder'ını çalıştırmaz; gerçek auth/role/antiforgery kontrolleri aktiftir. Parolalar her koşuda rastgele üretilir ve loglanmaz. Test dosyaları izole upload dizinlerine yazılır.

Browser testleri gerçek Edge ve MVC uygulamasını kullanır. Kestrel bridge test taşımasıdır; üretim TLS/cookie testi değildir. Görüntüler ve console/network kayıtları `MyAcademy_PortfolioProject.Tests/bin/Release/net9.0/browser-results/` altındadır; Git'e dahil edilmez.

Browser olmadan:

```powershell
dotnet test MyAcademy_PortfolioProject.sln -c Release --filter 'FullyQualifiedName!~Browser_&FullyQualifiedName!~Restored_copy_application_regression'
```

## SQL Server testleri — açık opt-in

Kendi sunucunuz için repo dışında veya ignore edilen `MyAcademy_Portfolio/appsettings.Local.json` içinde bir JSON hazırlayın. Dosya standart `ConnectionStrings:DefaultConnection` yapısını kullanır. Gerçek kullanıcı bilgilerini repoya eklemeyin. User Secrets uygulama çalışırken kullanılabilir; SQL test araçları ayrıca aşağıdaki dosya yolunu okur.

```powershell
$env:PORTFOLIO_SQL_TEST_CONFIG=(Resolve-Path MyAcademy_Portfolio/appsettings.Local.json).Path
dotnet test MyAcademy_PortfolioProject.sln -c Release
# SQL smoke aracı ayrı çalıştırılabilir:
dotnet run --project tools/SqlAudit -c Release -- --run MyAcademy_Portfolio/appsettings.Local.json
Remove-Item Env:PORTFOLIO_SQL_TEST_CONFIG
```

Testler bağlantı dosyasından sunucuyu alır, katalog adını her seferinde yeni `PortfolioPhase2_<GUID>` veya `PortfolioAudit_<GUID>` olarak değiştirir. Katalog oluşturma/migration yetkisi gerekir. Kaynak katalog fixture hedefi olmaz. Kataloglar korunur ve disk tüketir; kaldırılmaları ayrı, bilinçli bir işlemdir. Adlar test `sql-results/retained-catalogs.txt` çıktısındadır.

Dört SQL testi: temiz migration/Identity/eşzamanlı singleton ve uniqueness; duplicate veride güvenli migration reddi; dolu eski Skills yükseltmesinin beklenen engeli ve rollback; ayrı ad sonrası migration ve ilişki koruması. Legacy testi veri dönüşümünün başarılı olduğu anlamına gelmez.

## Gerçek restore kopyası testi — ileri seviye opt-in

`Restored_copy_application_regression` yalnızca önceden yedeklenmiş, doğrulanmış ve hedef migration uygulanmış ayrı bir restore ortamında çalıştırılır. Clone içinde gerçek yedek veya operation evidence bulunmaz; bu testin varsayılan olarak SKIP olması beklenir. Sadece bu testi yeşile çevirmek için gerçek veritabanını hedef göstermeyin veya evidence uydurmayın.

Gerekli dosyalar: `run.json` içindeki BackupVerified/RestoreVerified ve RestoredDatabase; `staging-success.json` içindeki aynı Catalog. Katalog adı `PortfolioRehearsal_<yyyyMMdd_HHmmss>_<GUID>` biçiminde olmalı; kaynak bağlantıyla aynı katalog olamaz. Fixture, proje/kategori ilişkileri ve ilgili kayıt ID'leri dahil belirli bir onaylı restore senaryosuna aittir; genel clone kurulum testi değildir.

```powershell
$env:PORTFOLIO_REHEARSAL_EVIDENCE = '<yerel-doğrulanmış-evidence-dizini>'
dotnet test MyAcademy_PortfolioProject.sln -c Release --filter 'FullyQualifiedName~Restored_copy_application_regression'
Remove-Item Env:PORTFOLIO_REHEARSAL_EVIDENCE
```

Test yalnızca kopyada yeni rastgele Identity hesabı oluşturur, gerçek login/logout ve kategori güncelleme/geri alma akışını çalıştırır, public filtreleri Edge'de kontrol eder. Geçici hesabı finally içinde kaldırır; mevcut hesapları/rolleri sıfırlamaz. Önce/sonra veri bütünlüğü ayrıca doğrulanmalıdır. Yerel, gerçek hedefe bağlı operasyon script'leri public repo kapsamı dışındadır.

## Migration model kontrolü

Testing ortamında bağlantı gerektiğinde kendi değerinizi process ortam değişkeniyle sağlayın; Development seeder'ını model incelemesi için çalıştırmayın.

```powershell
$env:ASPNETCORE_ENVIRONMENT='Testing'
dotnet ef migrations has-pending-model-changes --project MyAcademy_Portfolio --configuration Release --no-build
Remove-Item Env:ASPNETCORE_ENVIRONMENT
```

Kurulum ve güvenli Admin hazırlığı için kök `README.md` dosyasına bakın. Yerel özel audit raporları ve snapshot'lar yayına dahil edilmez.
