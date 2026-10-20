# BankStock

Website theo dõi và giao dịch giả lập cổ phiếu các ngân hàng Việt Nam, xây dựng bằng **ASP.NET Core MVC (.NET 8)**.

Ứng dụng hiển thị bảng giá, thông tin và báo cáo tài chính của 10 mã cổ phiếu ngân hàng (VCB, BID, CTG, TCB, MBB, ACB, VPB, STB, HDB, TPB). Nhà đầu tư được cấp **ví tiền ảo** để đặt lệnh mua/bán trên sổ lệnh giả lập. Ứng dụng không giao dịch tiền thật; dữ liệu giá là dữ liệu mẫu do quản trị viên nhập hoặc được seed sẵn.

## Tính năng

### Khách (chưa đăng nhập)

- Bảng giá và danh sách cổ phiếu: lọc theo nhóm (Big 4 / TMCP), P/E < 10, CASA > 30 %, cổ tức; sắp xếp và phân trang.
- Trang chi tiết mã với biểu đồ nến / biểu đồ miền theo khung 1D, 1W, 1M, 1Y.
- Thống kê toàn sàn.
- Đăng ký, đăng nhập, quên mật khẩu.

### Nhà đầu tư (Investor)

- Dashboard và Watchlist viết bằng AngularJS, lấy dữ liệu qua REST API.
- Ví ảo: nạp và rút tiền.
- Đặt lệnh Mua/Bán (Market / Limit), hủy lệnh đang chờ, giỏ lệnh nháp lưu trong Session.
- Danh mục đầu tư kèm lãi/lỗ chưa thực hiện, sổ lệnh, lịch sử khớp lệnh.

### Quản trị viên (Admin)

- CRUD Ngân hàng, Cổ phiếu, Lịch sử giá, Báo cáo tài chính (modal + AJAX + SweetAlert2).
- Chỉnh giá thủ công; các lệnh chờ thỏa điều kiện được khớp tự động.
- Quản lý người dùng: khóa/mở khóa, gán vai trò.
- Thống kê ví, xem toàn bộ lệnh, nhật ký hoạt động (audit log).

### Chung

- Giao diện sáng/tối (Light/Dark mode).
- Song ngữ **Tiếng Việt / English**, ngôn ngữ nằm trên URL (`/vi/...`, `/en/...`) và được ghi nhớ bằng cookie.
- REST API `/api/v1`, có Swagger UI trong môi trường Development.
- Bảo mật: chống CSRF, Content Security Policy và các security header khác, giới hạn tần suất request (rate limiting), chống truy cập dữ liệu của người khác (IDOR), khóa tài khoản khi đăng nhập sai nhiều lần.

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core MVC 8, ASP.NET Core Identity |
| Dữ liệu | Entity Framework Core 8 (Code-First), SQL Server |
| Giao diện | Bootstrap 5, jQuery, AngularJS 1.8 |
| Biểu đồ | Lightweight Charts, Chart.js |
| Khác | SweetAlert2, Swashbuckle (Swagger) |
| Kiểm thử | xUnit, EF Core InMemory |

## Cài đặt và chạy

### Yêu cầu

- [.NET SDK 8](https://dotnet.microsoft.com/download) trở lên (dự án target `net8.0`; `global.json` cho phép dùng SDK mới hơn).
- SQL Server **LocalDB** (cài kèm Visual Studio), SQL Server Express hoặc SQL Server chạy bằng Docker.
- Công cụ EF Core CLI:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

### Các bước

```bash
# 1. Khôi phục package
dotnet restore BankStock.slnx

# 2. Tạo cơ sở dữ liệu từ migration
#    (mặc định: (localdb)\mssqllocaldb, database BankStockDb)
dotnet ef database update -p BankStock.Web -s BankStock.Web

# 3. Chạy ứng dụng (môi trường Development tự seed dữ liệu mẫu ở lần chạy đầu)
dotnet run --project BankStock.Web --launch-profile https
```

Sau đó mở <https://localhost:7108> (hoặc <http://localhost:5173> nếu dùng profile `http`). Swagger UI nằm ở `/swagger`.

### Đổi chuỗi kết nối

Không ghi chuỗi kết nối thật vào repo. Dùng User Secrets:

```bash
dotnet user-secrets init --project BankStock.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=BankStockDb;Trusted_Connection=True;TrustServerCertificate=True" --project BankStock.Web
```

hoặc đặt biến môi trường `ConnectionStrings__DefaultConnection`.

### Chạy unit test

```bash
dotnet test BankStock.slnx
```

Các test bao phủ tầng nghiệp vụ (`OrderService`, `WalletService`, `TradingRules`, `PriceRules`, attribute `[LotSize]`) và chạy trên EF Core InMemory, nên **không cần SQL Server**.

## Tài khoản demo

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@bankstock.local` | `Admin@12345` |
| Investor | `investor1@bankstock.local` | `Investor@12345` |
| Investor | `investor2@bankstock.local` | `Investor@12345` |

Mỗi nhà đầu tư, kể cả tài khoản mới đăng ký, được cấp **500.000.000 VNĐ** tiền ảo. Đăng nhập sai 5 lần liên tiếp sẽ bị khóa tài khoản 15 phút.

## Cấu hình

Các khóa chính trong `BankStock.Web/appsettings.json`:

| Khóa | Mặc định | Ý nghĩa |
|---|---|---|
| `TradingFeePercentage` | `0.15` | Phí giao dịch (%) tính trên giá trị khớp |
| `InitialWalletGrant` | `500000000` | Số tiền ảo cấp khi tạo ví |
| `RateLimiting:Auth` | 10 request / 60 giây | Giới hạn đăng nhập, đăng ký, quên mật khẩu theo IP |
| `RateLimiting:Api` | 120 request / 60 giây | Giới hạn gọi `/api` theo người dùng (hoặc IP) |
| `Cors:AllowedOrigins` | `[]` | Các origin bên ngoài được phép gọi API |

## Cấu trúc thư mục

```text
BankStock.slnx
├── BankStock.Web/
│   ├── Areas/Admin/, Areas/Investor/   Controller + View theo vai trò
│   ├── Controllers/                    Trang công khai (Home, Account, Stocks)
│   ├── Controllers/Api/                REST API /api/v1
│   ├── Data/                           AppDbContext, SeedData
│   ├── Migrations/                     EF Core migrations
│   ├── Models/, ViewModels/, Dtos/     Entity, ViewModel, DTO
│   ├── Services/                       Nghiệp vụ (Controller → Service → DbContext)
│   ├── Filters/, Validation/           Action filter, validation attribute tùy chỉnh
│   ├── Localization/, Resources/       Culture trên URL, file .resx vi/en
│   ├── Security/                       Security header, chính sách rate limit
│   └── wwwroot/                        CSS, JavaScript (không dùng script inline)
└── BankStock.Tests/                    Unit test cho tầng Service
```

## Kiến trúc

Ứng dụng theo mô hình phân lớp: **Controller → Service → DbContext**. Controller chỉ nhận request, kiểm tra dữ liệu và trả về View/JSON; toàn bộ nghiệp vụ (khớp lệnh, trừ/cộng tiền ví, tính phí, cập nhật danh mục) nằm trong các Service và được kiểm thử độc lập. Các thao tác thay đổi số dư và khớp lệnh chạy trong transaction, có cơ chế thử lại khi xung đột đồng thời.
