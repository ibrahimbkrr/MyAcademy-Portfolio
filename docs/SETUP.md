# Yerel kurulum

## 10. Kurulum gereksinimleri

- .NET **9 SDK**; yalnızca runtime yeterli değildir.
- SQL Server / SQL Server Express veya Windows için LocalDB.
- .NET 9 destekli bir IDE ya da terminal.
- Migration komutları için EF Core 9 CLI; projede EF Core 9.0.20 kullanılır.
- Browser testleri için Microsoft Edge ve Playwright'ın gerektirdiği sistem bağımlılıkları.

.NET 9 hedefi bilerek korunmuştur; destek yaşam döngüsü riski nedeniyle bu repo üretime hazır yayın olarak değerlendirilmemelidir.

## 11. Yerel kurulum

Repoyu kendi GitHub adresinden klonlayın, solution köküne geçin ve paketleri yükleyin:

```powershell
dotnet restore MyAcademy_PortfolioProject.sln
dotnet tool install --global dotnet-ef --version 9.0.20
```

Araç zaten kuruluysa ikinci komut yerine sürümünü kontrol edin; gerektiğinde `dotnet tool update --global dotnet-ef --version 9.0.20` kullanın.

Bağlantı bilgisi repodaki `appsettings.json` içinde boş bırakılmıştır. Kendi SQL Server bağlantınızı User Secrets'a kaydedin. Aşağıdaki PowerShell akışı değeri komut geçmişine yazmadan alır:

```powershell
$connectionInput = Read-Host 'Kendi SQL Server bağlantı dizenizi girin' -AsSecureString
$env:ConnectionStrings__DefaultConnection = [System.Net.NetworkCredential]::new('', $connectionInput).Password
@{ 'ConnectionStrings:DefaultConnection' = $env:ConnectionStrings__DefaultConnection } |
    ConvertTo-Json | dotnet user-secrets set --project MyAcademy_Portfolio
```

Yeni veritabanına migration uygularken Development seeder'ını kapatın. Connection string yukarıdaki process ortam değişkeninden okunur; Testing ortamında User Secrets varsayılan olarak yüklenmez:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Testing'
$env:DOTNET_ENVIRONMENT = 'Testing'
dotnet ef database update --project MyAcademy_Portfolio
# Başarıdan sonra geçici ortam değerlerini temizleyin:
Remove-Item Env:ConnectionStrings__DefaultConnection
Remove-Item Env:ASPNETCORE_ENVIRONMENT
Remove-Item Env:DOTNET_ENVIRONMENT
```

Komut başarısız olursa veritabanını silerek devam etmeyin; hatayı ve hedef bağlantıyı inceleyin. Bu komutlar sizin seçtiğiniz veritabanında schema oluşturur; bağlantıyı çalıştırmadan önce kontrol edin.

## 12. Sırsız yapılandırma örneği

Aşağıdaki bağlantı yalnızca yerel Windows bütünleşik kimlik doğrulaması örneğidir; gerçek kullanıcı bilgisi içermez:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MyAcademyPortfolio_Local;Integrated Security=True;TrustServerCertificate=True"
  }
}
```

Kendi sunucunuza göre uyarlayın. `TrustServerCertificate=True` yerel geliştirme örneğidir; canlı ortam TLS doğrulamasının yerine geçmez. User Secrets geliştirme kolaylığı sağlar, şifreli bir kasa değildir. Production sırlarını repoya veya bu dosyaya koymayın.

İsteğe bağlı anahtarlar: `Uploads:RootDirectory`, `DataProtection:KeyDirectory`, `ReverseProxy:TrustedProxies`. Gerçek dizin/IP değerlerini yerel secret/ortam ayarlarında tutun. [Hosting hazırlık notları](../README.md#proje-durumu) canlı ortamda henüz doğrulanmamış adımları açıklar.

## 13. Yerel Admin hesabını güvenli oluşturma

Migration tamamlandıktan sonra kendi e-posta ve parolanızı girin. Parola en az 12 karakter olmalı; büyük/küçük harf, rakam ve özel karakter içermelidir. Repo bir test parolası veya paylaşılan Admin hesabı sağlamaz.

```powershell
$adminEmailInput = Read-Host 'Yerel Admin e-posta adresiniz'
$adminPasswordInput = Read-Host 'Yerel Admin parolanız' -AsSecureString
@{
  'AdminSeed:Email' = $adminEmailInput
  'AdminSeed:Password' = [System.Net.NetworkCredential]::new('', $adminPasswordInput).Password
} | ConvertTo-Json | dotnet user-secrets set --project MyAcademy_Portfolio
```

Uygulamayı Development ortamında ilk açışta Admin rolü ve belirtilen yeni hesap oluşturulur. Hesap kurulduktan ve giriş doğrulandıktan sonra uygulamayı durdurup seed değerlerini kaldırabilirsiniz; mevcut hesap silinmez:

```powershell
dotnet user-secrets remove 'AdminSeed:Password' --project MyAcademy_Portfolio
dotnet user-secrets remove 'AdminSeed:Email' --project MyAcademy_Portfolio
```

Sadece yapılandırmayı değiştirmek mevcut kullanıcı parolasını değiştirmez. Başkasına ait mevcut bir hesap adresini seed hedefi olarak kullanmayın.

## 14. Uygulamayı çalıştırma

```powershell
dotnet dev-certs https --trust
dotnet run --project MyAcademy_Portfolio --launch-profile https
```

Portları terminal çıktısından veya `Properties/launchSettings.json` dosyasından kontrol edin. Public sayfa `/`, giriş ekranı `/Admin/Account/Login`, yönetim alanı `/Admin` yolundadır. Güvenli cookie kullanıldığı için Admin akışlarında HTTPS profilini tercih edin.

Boş veritabanında kendi içeriklerinizi Admin ekranlarından ekleyin; kişisel geliştirme verileri clone ile gelmez. `/health/live` süreç sağlığını, `/health/ready` SQL erişimini kontrol eder. Public Contact formunda istemci IP'si başına 10 dakikada 5 gönderim sınırı vardır; bu kota process-local'dır.
