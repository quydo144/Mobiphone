# Hướng Dẫn Release

## Quy Trình Tự Động

Dự án này sử dụng GitHub Actions để tự động build, test và tạo MSI installer.

### Build và Test thường xuyên

Mỗi khi bạn push code lên branch `main` hoặc `master`, hoặc tạo Pull Request:
- GitHub Actions sẽ tự động build project
- Chạy các test cases (nếu có)
- Thông báo lỗi nếu build fail

### Release với MSI Installer

Khi muốn release phiên bản mới cho user:

1. **Cập nhật version trong project** (nếu cần):
   ```xml
   <!-- Trong Mobiphone.csproj -->
   <Version>1.0.0</Version>
   ```

2. **Tạo và push tag với prefix `v`**:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. **GitHub Actions sẽ tự động**:
   - Build ứng dụng với cấu hình Release
   - Tạo file MSI installer cho Windows 64-bit
   - Tạo GitHub Release với tag tương ứng
   - Upload file MSI vào Release
   - Tạo artifact để download

4. **User có thể**:
   - Vào trang Releases của repository
   - Download file `Mobiphone.msi`
   - Double-click để cài đặt
   - Ứng dụng sẽ được cài vào Program Files
   - Shortcut tự động tạo trong Start Menu

## Test Local trước khi Release

Trước khi push tag lên GitHub, bạn nên test toàn bộ quy trình build ở local:

### Chạy script test tự động:
```powershell
.\test-release.ps1
```

Script này thực hiện:
1. Restore dependencies
2. Build project (Release configuration)
3. Run tests
4. Publish application (self-contained, single-file)
5. Build MSI installer với WiX

### Kết quả:
- ✅ `bin\Release\net8.0-windows\win-x64\publish\Mobiphone.exe` - Executable
- ✅ `Mobiphone.msi` - MSI installer

### Test MSI installer:
```powershell
# Install locally để test
.\Mobiphone.msi

# Sau khi test xong, uninstall qua Settings -> Apps
```

**Lưu ý**: Nếu có lỗi, sửa và test lại trước khi push tag.

## Ví Dụ Release

```bash
# Commit thay đổi cuối cùng
git add .
git commit -m "Ready for v1.0.0 release"
git push

# Tạo tag và push
git tag v1.0.0
git push origin v1.0.0
```

## Kiểm Tra Workflow

Xem trạng thái workflow tại:
- Repository → Actions tab
- Xem chi tiết build log
- Download artifacts nếu cần test trước

## Format Tag

- ✅ `v1.0.0` - Đúng format, sẽ trigger release
- ✅ `v2.1.3` - Đúng format, sẽ trigger release
- ❌ `1.0.0` - Thiếu prefix 'v', không trigger release
- ❌ `release-1.0` - Sai format, không trigger release

## Yêu Cầu

- Repository phải được đẩy lên GitHub
- GitHub Actions phải được enable
- Có quyền tạo tags và releases
- Windows runner available (mặc định có sẵn)

## Troubleshooting

### Workflow không chạy
- Kiểm tra GitHub Actions có enabled không
- Kiểm tra file `.github/workflows/build-release.yml` đã được commit

### MSI không được tạo
- Kiểm tra logs trong Actions tab
- Đảm bảo tag bắt đầu với 'v'
- Kiểm tra WiX configuration

### Release không tự động tạo
- Kiểm tra `GITHUB_TOKEN` permissions
- Xem error logs trong workflow run
