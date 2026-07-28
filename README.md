# SC Hoang Quoc

Ứng dụng WPF để quản lý và phân loại số điện thoại từ file Excel vào SQLite database với tính năng phân loại sim đặc biệt, filter nâng cao và hiệu suất cao.

## Yêu cầu

- .NET 8.0 Runtime (sẽ được cài tự động khi cài MSI)
- Windows 10/11 (64-bit)

## Cài đặt cho người dùng cuối

### Download và cài đặt từ MSI (Khuyến nghị)

1. Truy cập trang [Releases](../../releases) của repository
2. Tải file `SC Hoang Quoc.msi` từ phiên bản mới nhất
3. Double-click file MSI và làm theo hướng dẫn
4. Ứng dụng sẽ được cài đặt vào `C:\Program Files\SC Hoang Quoc`
5. Shortcut tự động xuất hiện trong Start Menu

**Lưu ý**: Windows Defender có thể cảnh báo vì đây là ứng dụng chưa được ký số. Chọn "More info" → "Run anyway" để tiếp tục.

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

## Packages sử dụng

- **EPPlus 7.0.5**: Đọc và xử lý file Excel
- **System.Data.SQLite.Core 1.0.118**: Làm việc với SQLite database
- **System.Windows.Forms**: OpenFileDialog và FolderBrowserDialog

## Database

Database SQLite sẽ được tạo tự động tại thư mục bin với tên `records.db`.

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

### Test release build locally

Trước khi push tag, bạn có thể test toàn bộ quy trình release ở local:

```powershell
.\test-release.ps1
```

Script này sẽ:
1. ✅ Restore dependencies
2. ✅ Build project (Release mode)
3. ✅ Run tests
4. ✅ Publish application (single-file exe)
5. ✅ Build MSI installer

Kết quả:
- `bin\Release\net8.0-windows\win-x64\publish\SC Hoang Quoc.exe` - Single-file executable
- `SC Hoang Quoc.msi` - MSI installer

### Cleanup các file tạm thời

Sau khi build MSI, script tự động cleanup các file không cần thiết. Nếu cần cleanup thủ công:

```powershell
.\cleanup.ps1
```

Script này sẽ xóa:
- `bin/` và `obj/` folders (build artifacts)
- `*.wixpdb` files (WiX debug files)
- Test MSI files

### Push release lên GitHub

Sau khi test thành công ở local:

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
