# Excel to SQLite Importer - Mobiphone SIM Manager

Ứng dụng WPF để quản lý và phân loại số điện thoại từ file Excel vào SQLite database với tính năng phân loại sim đặc biệt, filter nâng cao và hiệu suất cao.

## Tính năng

### Import & Database
- ✅ **Import file Excel** - Chọn từng file hoặc cả folder
- ✅ **Batch Import** - Scan và import tất cả file Excel trong folder
- ✅ **Phone UNIQUE** - Tự động skip số trùng lặp
- ✅ **Hỗ trợ phone 9-10 số** - Tự động bỏ số 0 đầu
- ✅ **Progress Bar** - Hiển thị tiến trình import chi tiết
- ✅ **Index trên Phone** - Query nhanh chóng
- ✅ **Thống kê chi tiết** - imported, duplicates, errors

### Phân loại sim đặc biệt
- ✅ **MINI thần tài** - Số đuôi 39
- ✅ **BIG Thần tài** - Số đuôi 79
- ✅ **Lộc phát** - Số đuôi 68
- ✅ **Tứ quý** - 4 số cuối giống nhau (IJKL)
- ✅ **Tứ quý giữa** - 4 số liên tiếp giống nhau ở vị trí bất kỳ
- ✅ **Tam hoa kép** - 3 số giống nhau + 3 số giống nhau (GHI = JKL)

### Filter & Search
- ✅ **Lọc theo Phone** - Tìm kiếm một phần số (LIKE)
- ✅ **Lọc theo loại sim** - Chọn một hoặc nhiều loại (OR logic)
- ✅ **Apply/Clear Filter** - Dễ dàng lọc và xóa lọc
- ✅ **Filter State Management** - Giữ filter khi phân trang
- ✅ **Hiển thị kết quả** - "Found X records" sau khi filter
- ✅ **Total Records** - Luôn hiển thị tổng số records trong DB

### UI & Display
- ✅ **Layout 2 cột tối ưu**: Filter (60%) | Import (40%)
- ✅ **Pagination hiệu năng cao** - 50/100/500/1000 records/page
- ✅ **Navigation mượt mà** - First, Prev, Next, Last
- ✅ **DataGrid với virtualization** - Xử lý hàng nghìn records mượt
- ✅ **Checkbox display** - Hiển thị loại sim trực quan
- ✅ **GridLines rõ ràng** - Dễ phân biệt cột và hàng
- ✅ **Alternating rows** - Màu xen kẽ cho dễ đọc
- ✅ **Responsive** - Tự động điều chỉnh kích thước

### Performance Optimizations
- ✅ **Row Virtualization** - Chỉ render rows hiển thị
- ✅ **Column Virtualization** - Giảm tải rendering
- ✅ **Recycling Mode** - Tái sử dụng rows
- ✅ **Pixel Scrolling** - Scroll mượt mà
- ✅ **Cache Optimization** - Cache 20 items trước/sau
- ✅ **Async Operations** - Không block UI thread

## Yêu cầu

- .NET 8.0 Runtime (sẽ được cài tự động khi cài MSI)
- Windows 10/11 (64-bit)

## Cài đặt cho người dùng cuối

### Download và cài đặt từ MSI (Khuyến nghị)

1. Truy cập trang [Releases](../../releases) của repository
2. Tải file `Mobiphone.msi` từ phiên bản mới nhất
3. Double-click file MSI và làm theo hướng dẫn
4. Ứng dụng sẽ được cài đặt vào `C:\Program Files\ExcelToSQLite`
5. Shortcut tự động xuất hiện trong Start Menu

**Lưu ý**: Windows Defender có thể cảnh báo vì đây là ứng dụng chưa được ký số. Chọn "More info" → "Run anyway" để tiếp tục.

## Phát triển (Development)

### Yêu cầu
- .NET 8.0 SDK hoặc cao hơn
- Windows OS

## Cài đặt và chạy

1. Mở terminal và chạy lệnh để restore packages:
```powershell
dotnet restore
```

2. Build project:
```powershell
dotnet build
```

3. Chạy ứng dụng:
```powershell
dotnet run
```

## Cấu trúc file Excel

File Excel cần có cấu trúc như sau (dòng đầu tiên là header):

| Phone |
|------------|
| 0901234567 |
| 0912345678 |
| 0923456789 |

**Lưu ý**: 
- Phone có thể là 9 hoặc 10 số
- Nếu 10 số bắt đầu bằng 0, số 0 sẽ được bỏ đi tự động
- Chỉ lưu phone có đúng 9 chữ số vào database
- Hỗ trợ nhiều file Excel trong cùng một folder

## Cấu trúc project

```
Mobiphone/
├── Models/
│   ├── Record.cs              # Model class (Id, Phone, 6 sim types)
│   └── RecordFilterOptions.cs # Filter options model
├── Services/
│   ├── ExcelService.cs        # Service đọc file Excel
│   └── DatabaseService.cs     # Service xử lý SQLite
├── bin/                       # Build output
├── obj/                       # Build intermediates
├── App.xaml                   # Application definition
├── App.xaml.cs                # Application code-behind
├── MainWindow.xaml            # Main UI với pagination & filter
├── MainWindow.xaml.cs         # Main window code-behind
├── ExcelToSQLite.csproj       # Project file
├── Mobiphone.sln              # Solution file
├── README.md                  # Documentation
└── TODO.md                    # Development history
```

## Packages sử dụng

- **EPPlus 7.0.5**: Đọc và xử lý file Excel
- **System.Data.SQLite.Core 1.0.118**: Làm việc với SQLite database
- **System.Windows.Forms**: OpenFileDialog và FolderBrowserDialog

## Database

Database SQLite sẽ được tạo tự động tại thư mục bin với tên `records.db`.

### Bảng Records:
- **Id** (INTEGER PRIMARY KEY AUTOINCREMENT)
- **Phone** (TEXT NOT NULL UNIQUE) - Số điện thoại 9 chữ số
- **MiniThanTai** (INTEGER) - 0/1 cho đuôi 39
- **BigThanTai** (INTEGER) - 0/1 cho đuôi 79
- **LocPhat** (INTEGER) - 0/1 cho đuôi 68
- **TuQuy** (INTEGER) - 0/1 cho 4 số cuối giống nhau
- **TuQuyGiua** (INTEGER) - 0/1 cho 4 số giống nhau ở giữa
- **TamHoaKep** (INTEGER) - 0/1 cho 3+3 số giống nhau

### Index:
- **idx_phone** ON Records(Phone) - Tăng tốc query

## Hướng dẫn sử dụng

### Import dữ liệu (Bên phải):

#### Import từ file:
1. Click nút **📁 Browse File** để chọn file Excel
2. Click nút **▶ Import** để bắt đầu import
3. Xem tiến trình và kết quả trong status text

#### Import từ folder:
1. Click nút **📂 Browse Folder** để chọn folder chứa nhiều file Excel
2. Ứng dụng sẽ tự động scan tất cả file *.xls và *.xlsx
3. Click nút **▶ Import** để import tất cả files
4. Xem tiến trình và thống kê chi tiết:
   - Files processed
   - Total phone numbers read
   - Successfully imported
   - Duplicates skipped
   - Files with errors (nếu có)

### Filter dữ liệu (Bên trái):
1. **Lọc theo Phone**: Nhập một phần số điện thoại
2. **Lọc theo loại sim**: Chọn một hoặc nhiều checkbox
3. Click **🔍 Apply Filter** để áp dụng
4. Click **Clear** để xóa bộ lọc
5. Kết quả: "Filter applied. Found X records."
6. Filter được giữ nguyên khi phân trang

### Xem dữ liệu:
- **DataGrid**: Hiển thị Phone + 6 loại sim (checkbox)
- **Total Records**: Tổng số records trong database
- **Pagination**: Chọn 50/100/500/1000 records/page
- **Navigation**: First, Prev, Next, Last với filter state
- **Smooth Scrolling**: Virtualization cho hiệu suất cao

## Tính năng Phone Unique

- Mỗi số điện thoại chỉ tồn tại **một lần duy nhất** trong database
- Import cùng 1 file nhiều lần: chỉ phone mới được thêm vào
- Tự động báo cáo số phone trùng lặp
- Không cần kiểm tra file hash hay lịch sử import

## Debug với VS Code

- Nhấn **F5** để bắt đầu debug
- Đặt breakpoint bằng cách click vào lề trái
- Xem giá trị biến, step through code

## Release mới (Cho Developer)

Để tạo phiên bản release mới với MSI installer:

```bash
# Commit các thay đổi
git add .
git commit -m "Your changes"
git push

# Tạo tag với format v*
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions sẽ tự động:
- Build và test ứng dụng
- Tạo file MSI installer
- Tạo GitHub Release
- Upload MSI file để người dùng download

Chi tiết xem file [RELEASE.md](RELEASE.md).

## CI/CD

Project sử dụng GitHub Actions để:
- **Build & Test**: Tự động chạy mỗi khi push code hoặc tạo PR
- **Create Release**: Tự động tạo MSI installer khi push tags v*
- **Artifact Upload**: Lưu trữ MSI files trong GitHub Releases

Xem workflow tại [.github/workflows/build-release.yml](.github/workflows/build-release.yml)
