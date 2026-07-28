using System.IO;
using System.Windows;
using Mobiphone.Services;
using Mobiphone.Models;
using WinForms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace Mobiphone
{
    public partial class MainWindow : Window
    {
        private readonly ExcelService _excelService;
        private readonly DatabaseService _databaseService;
        private string _selectedFilePath = string.Empty;
        private string _selectedFolderPath = string.Empty;
        private bool _isFolder = false;
        private int _currentPage = 1;
        private int _pageSize = 100;
        private int _totalRecords = 0;
        private int _filteredRecords = 0;
        private int _totalPages = 0;
        private bool _isInitialized = false;

        // Store current filter state
        private RecordFilterOptions _currentFilter = new RecordFilterOptions();

        public MainWindow()
        {
            InitializeComponent();
            _excelService = new ExcelService();
            _databaseService = new DatabaseService();

            // Initialize database
            _databaseService.InitializeDatabase();
            _isInitialized = true;
            LoadDataFromDatabase();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var openFileDialog = new WinForms.OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xlsx;*.xls|All Files|*.*";
                openFileDialog.Title = "Chọn file Excel";

                if (openFileDialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    _selectedFilePath = openFileDialog.FileName;
                    _selectedFolderPath = string.Empty;
                    _isFolder = false;
                    txtFilePath.Text = _selectedFilePath;

                    btnImport.IsEnabled = true;
                    txtStatus.Text = "Đã chọn file. Sẵn sàng nhập dữ liệu.";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
        }

        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new WinForms.FolderBrowserDialog())
            {
                folderDialog.Description = "Chọn thư mục chứa các file Excel";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    _selectedFolderPath = folderDialog.SelectedPath;
                    _selectedFilePath = string.Empty;
                    _isFolder = true;

                    // Count Excel files in folder
                    var excelFiles = Directory.GetFiles(_selectedFolderPath, "*.xls*", SearchOption.TopDirectoryOnly);
                    txtFilePath.Text = _selectedFolderPath;

                    btnImport.IsEnabled = excelFiles.Length > 0;

                    if (excelFiles.Length > 0)
                    {
                        txtStatus.Text = $"Đã chọn thư mục. Tìm thấy {excelFiles.Length} file Excel. Sẵn sàng nhập dữ liệu.";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    }
                    else
                    {
                        txtStatus.Text = "Không tìm thấy file Excel nào trong thư mục đã chọn.";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    }
                }
            }
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath) && string.IsNullOrEmpty(_selectedFolderPath))
            {
                MessageBox.Show("Vui lòng chọn một file hoặc thư mục trước.", "Cảnh báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isFolder)
            {
                await ImportFromFolder();
            }
            else
            {
                await ImportFromFile(_selectedFilePath);
            }
        }

        private async Task ImportFromFile(string filePath)
        {
            try
            {
                btnImport.IsEnabled = false;
                btnBrowse.IsEnabled = false;
                btnBrowseFolder.IsEnabled = false;

                // Show progress bar
                progressBar.Visibility = Visibility.Visible;
                txtProgress.Visibility = Visibility.Visible;
                progressBar.Value = 0;

                string fileName = Path.GetFileName(filePath);
                txtStatus.Text = $"Đang xử lý: {fileName}...";
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;

                // Read Excel file
                progressBar.Value = 20;
                txtProgress.Text = "Đang đọc file Excel...";
                var records = await Task.Run(() => _excelService.ReadExcelFile(filePath));

                if (records.Count == 0)
                {
                    progressBar.Visibility = Visibility.Collapsed;
                    txtProgress.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Không tìm thấy số điện thoại hợp lệ trong file Excel.\n\nSố điện thoại phải chính xác 9 chữ số.", "Thông tin",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    txtStatus.Text = "Không có dữ liệu hợp lệ để nhập.";
                    return;
                }

                // Import to database
                progressBar.Value = 50;
                txtProgress.Text = $"Đang nhập {records.Count} số điện thoại vào cơ sở dữ liệu...";
                var (importedCount, duplicateCount) = await Task.Run(() => _databaseService.ImportRecords(records));

                // Reload data
                progressBar.Value = 90;
                txtProgress.Text = "Đang tải lại dữ liệu...";
                LoadDataFromDatabase();

                // Complete
                progressBar.Value = 100;
                txtProgress.Text = "Nhập dữ liệu hoàn tất!";
                await Task.Delay(500); // Show 100% briefly

                progressBar.Visibility = Visibility.Collapsed;
                txtProgress.Visibility = Visibility.Collapsed;

                string statusMessage = $"Đã nhập thành công {importedCount} số điện thoại từ {fileName}";
                if (duplicateCount > 0)
                {
                    statusMessage += $" ({duplicateCount} bản sao đã bị bỏ qua)";
                }
                txtStatus.Text = statusMessage;
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;

                string messageDetails = $"Nhập dữ liệu thành công!\n\n" +
                    $"File: {fileName}\n" +
                    $"Tổng số điện thoại trong file: {records.Count}\n" +
                    $"Đã nhập thành công: {importedCount}\n";

                if (duplicateCount > 0)
                {
                    messageDetails += $"Bản sao đã bị bỏ qua: {duplicateCount}";
                }

                MessageBox.Show(
                    messageDetails,
                    "Nhập Dữ Liệu Hoàn Tất",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressBar.Visibility = Visibility.Collapsed;
                txtProgress.Visibility = Visibility.Collapsed;
                txtStatus.Text = "Nhập dữ liệu thất bại!";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Lỗi khi nhập dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                progressBar.Value = 0;
                btnImport.IsEnabled = true;
                btnBrowse.IsEnabled = true;
                btnBrowseFolder.IsEnabled = true;
            }
        }

        private async Task ImportFromFolder()
        {
            try
            {
                btnImport.IsEnabled = false;
                btnBrowse.IsEnabled = false;
                btnBrowseFolder.IsEnabled = false;

                // Get all Excel files
                var excelFiles = Directory.GetFiles(_selectedFolderPath, "*.xls*", SearchOption.TopDirectoryOnly);

                if (excelFiles.Length == 0)
                {
                    MessageBox.Show("Không tìm thấy file Excel nào trong thư mục đã chọn.", "Thông tin",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show progress bar
                progressBar.Visibility = Visibility.Visible;
                txtProgress.Visibility = Visibility.Visible;
                progressBar.Value = 0;

                int totalImported = 0;
                int totalDuplicates = 0;
                int totalRecordsRead = 0;
                int filesProcessed = 0;
                int filesWithErrors = 0;
                var errorFiles = new List<string>();

                txtStatus.Text = $"Đang xử lý {excelFiles.Length} file Excel...";
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;

                foreach (var filePath in excelFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        filesProcessed++;

                        // Update progress
                        progressBar.Value = (filesProcessed * 100.0 / excelFiles.Length);
                        txtProgress.Text = $"Đang xử lý file {filesProcessed}/{excelFiles.Length}: {fileName}";

                        // Read Excel file
                        var records = await Task.Run(() => _excelService.ReadExcelFile(filePath));
                        totalRecordsRead += records.Count;

                        if (records.Count > 0)
                        {
                            // Import to database
                            var (importedCount, duplicateCount) = await Task.Run(() => _databaseService.ImportRecords(records));
                            totalImported += importedCount;
                            totalDuplicates += duplicateCount;
                        }
                    }
                    catch (Exception ex)
                    {
                        filesWithErrors++;
                        errorFiles.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                // Reload data
                txtProgress.Text = "Đang tải lại dữ liệu...";
                LoadDataFromDatabase();

                // Complete
                progressBar.Value = 100;
                await Task.Delay(500);

                progressBar.Visibility = Visibility.Collapsed;
                txtProgress.Visibility = Visibility.Collapsed;

                string statusMessage = $"Nhập thư mục hoàn tất! Đã nhập {totalImported} số điện thoại từ {filesProcessed} file";
                if (totalDuplicates > 0)
                {
                    statusMessage += $" ({totalDuplicates} bản sao đã bị bỏ qua)";
                }
                txtStatus.Text = statusMessage;
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;

                string messageDetails = $"Nhập thư mục hoàn tất!\n\n" +
                    $"Files đã xử lý: {filesProcessed}\n" +
                    $"Tổng số điện thoại đọc được: {totalRecordsRead}\n" +
                    $"Đã nhập thành công: {totalImported}\n" +
                    $"Bản sao đã bỏ qua: {totalDuplicates}\n";

                if (filesWithErrors > 0)
                {
                    messageDetails += $"\nFiles with errors: {filesWithErrors}\n";
                    messageDetails += string.Join("\n", errorFiles.Take(5));
                    if (errorFiles.Count > 5)
                    {
                        messageDetails += $"\n... and {errorFiles.Count - 5} more";
                    }
                }

                MessageBox.Show(
                    messageDetails,
                    "Nhập Thư Mục Hoàn Tất",
                    MessageBoxButton.OK,
                    filesWithErrors > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressBar.Visibility = Visibility.Collapsed;
                txtProgress.Visibility = Visibility.Collapsed;
                txtStatus.Text = "Nhập thư mục thất bại!";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Lỗi khi nhập thư mục: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                progressBar.Value = 0;
                btnImport.IsEnabled = true;
                btnBrowse.IsEnabled = true;
                btnBrowseFolder.IsEnabled = true;
            }
        }

        private void LoadDataFromDatabase(RecordFilterOptions? filterOptions = null)
        {
            // Save current filter state
            if (filterOptions != null)
            {
                _currentFilter = filterOptions;
            }

            try
            {
                // Get filtered count for pagination
                _filteredRecords = _databaseService.GetRecordCount(_currentFilter);
                _totalPages = (_filteredRecords + _pageSize - 1) / _pageSize; // Ceiling division

                if (_totalPages == 0) _totalPages = 1;
                if (_currentPage > _totalPages) _currentPage = _totalPages;
                if (_currentPage < 1) _currentPage = 1;

                // Get paginated records with filters
                var records = _databaseService.GetRecordsWithPagination(
                    _currentPage, _pageSize, _currentFilter);
                dgData.ItemsSource = records;

                // Get total count in database (without filter) for Total Records display
                _totalRecords = _databaseService.GetRecordCount(new RecordFilterOptions());

                // Update UI
                txtRecordCount.Text = _totalRecords.ToString();
                txtPageInfo.Text = $"Trang {_currentPage} của {_totalPages}";

                // Update button states
                btnFirstPage.IsEnabled = _currentPage > 1;
                btnPrevPage.IsEnabled = _currentPage > 1;
                btnNextPage.IsEnabled = _currentPage < _totalPages;
                btnLastPage.IsEnabled = _currentPage < _totalPages;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFirstPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadDataFromDatabase();
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadDataFromDatabase();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadDataFromDatabase();
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages;
            LoadDataFromDatabase();
        }

        private void CmbPageSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!_isInitialized || cmbPageSize.SelectedItem == null)
                return;

            var selectedItem = (System.Windows.Controls.ComboBoxItem)cmbPageSize.SelectedItem;
            _pageSize = int.Parse(selectedItem.Content.ToString() ?? "100");
            _currentPage = 1; // Reset to first page when page size changes
            LoadDataFromDatabase();
        }

        private void BtnApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create filter options from UI controls
                var filterOptions = new RecordFilterOptions
                {
                    PhoneFilter = string.IsNullOrWhiteSpace(txtFilterPhone.Text) ? "" : txtFilterPhone.Text.Trim(),
                    FilterTuQuy = chkFilterTuQuy.IsChecked == true,
                    FilterTaxi2 = chkFilterTaxi2.IsChecked == true,
                    FilterTaxi3 = chkFilterTaxi3.IsChecked == true,
                    FilterTaxi4 = chkFilterTaxi4.IsChecked == true,
                    FilterTaxi5 = chkFilterTaxi5.IsChecked == true,
                    FilterTaxiDu2 = chkFilterTaxiDu2.IsChecked == true,
                    FilterTaxiDu3 = chkFilterTaxiDu3.IsChecked == true,
                    FilterDuoiTien = chkFilterDuoiTien.IsChecked == true,
                    FilterSanhGiua = chkFilterSanhGiua.IsChecked == true,
                    FilterTamHoa = chkFilterTamHoa.IsChecked == true,
                    FilterSoiGuong = chkFilterSoiGuong.IsChecked == true,
                    FilterAXA_AYA = chkFilterAXA_AYA.IsChecked == true,
                    FilterAXA_BXB = chkFilterAXA_BXB.IsChecked == true,
                    FilterAXA_BYB = chkFilterAXA_BYB.IsChecked == true,
                    FilterABABAC = chkFilterABABAC.IsChecked == true,
                    FilterABACAC = chkFilterABACAC.IsChecked == true
                };

                // Reset to first page when applying filter
                _currentPage = 1;

                // Load data with filters
                LoadDataFromDatabase(filterOptions);

                txtFilterStatus.Text = $"Bộ lọc đã được áp dụng. Tìm thấy {_filteredRecords} bản ghi.";
                txtFilterStatus.Foreground = System.Windows.Media.Brushes.Blue;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi áp dụng bộ lọc: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear all filter controls
                txtFilterPhone.Clear();

                chkFilterTuQuy.IsChecked = false;
                chkFilterTaxi2.IsChecked = false;
                chkFilterTaxi3.IsChecked = false;
                chkFilterTaxi4.IsChecked = false;
                chkFilterTaxi5.IsChecked = false;
                chkFilterTaxiDu2.IsChecked = false;
                chkFilterTaxiDu3.IsChecked = false;
                chkFilterDuoiTien.IsChecked = false;
                chkFilterSanhGiua.IsChecked = false;
                chkFilterTamHoa.IsChecked = false;
                chkFilterSoiGuong.IsChecked = false;
                chkFilterAXA_AYA.IsChecked = false;
                chkFilterAXA_BXB.IsChecked = false;
                chkFilterAXA_BYB.IsChecked = false;
                chkFilterABABAC.IsChecked = false;
                chkFilterABACAC.IsChecked = false;

                // Reset to first page
                _currentPage = 1;

                // Load all data without filters
                LoadDataFromDatabase(new RecordFilterOptions());

                txtFilterStatus.Text = $"Bộ lọc đã được xóa. Đang hiển thị tất cả {_totalRecords} bản ghi.";
                txtFilterStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa bộ lọc: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgData_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Check if Ctrl+C is pressed
            if (e.Key == System.Windows.Input.Key.C &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                CopySelectedPhoneNumber();
                e.Handled = true; // Prevent default DataGrid copy behavior
            }
        }

        private void CopySelectedPhoneNumber()
        {
            if (dgData.SelectedItem is Record selectedRecord)
            {
                try
                {
                    System.Windows.Clipboard.SetText(selectedRecord.Phone);
                    txtFilterStatus.Text = $"Đã sao chép: {selectedRecord.Phone}";
                    txtFilterStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi sao chép vào clipboard: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filterOptions = new RecordFilterOptions
                {
                    PhoneFilter = string.IsNullOrWhiteSpace(txtFilterPhone.Text) ? "" : txtFilterPhone.Text.Trim(),
                    FilterTuQuy = chkFilterTuQuy.IsChecked == true,
                    FilterTaxi2 = chkFilterTaxi2.IsChecked == true,
                    FilterTaxi3 = chkFilterTaxi3.IsChecked == true,
                    FilterTaxi4 = chkFilterTaxi4.IsChecked == true,
                    FilterTaxi5 = chkFilterTaxi5.IsChecked == true,
                    FilterTaxiDu2 = chkFilterTaxiDu2.IsChecked == true,
                    FilterTaxiDu3 = chkFilterTaxiDu3.IsChecked == true,
                    FilterDuoiTien = chkFilterDuoiTien.IsChecked == true,
                    FilterSanhGiua = chkFilterSanhGiua.IsChecked == true,
                    FilterTamHoa = chkFilterTamHoa.IsChecked == true,
                    FilterSoiGuong = chkFilterSoiGuong.IsChecked == true,
                    FilterAXA_AYA = chkFilterAXA_AYA.IsChecked == true,
                    FilterAXA_BXB = chkFilterAXA_BXB.IsChecked == true,
                    FilterAXA_BYB = chkFilterAXA_BYB.IsChecked == true,
                    FilterABABAC = chkFilterABABAC.IsChecked == true,
                    FilterABACAC = chkFilterABACAC.IsChecked == true
                };

                _currentFilter = filterOptions;
                var recordsToExport = _databaseService.GetAllFilteredRecords(filterOptions);

                if (recordsToExport == null || recordsToExport.Count == 0)
                {
                    MessageBox.Show("Không có bản ghi nào để xuất dựa trên bộ lọc hiện tại.", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Lưu dữ liệu xuất Excel",
                    FileName = GenerateExportFileName(filterOptions)
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string exportPath = saveFileDialog.FileName;

                    await Task.Run(() => _excelService.ExportToExcel(recordsToExport, exportPath));

                    MessageBox.Show($"Xuất dữ liệu thành công ra file:\n{exportPath}", "Hoàn tất",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateExportFileName(RecordFilterOptions filter)
        {
            var activeFilters = new List<string>();
            if (!string.IsNullOrWhiteSpace(filter.PhoneFilter))
            {
                activeFilters.Add($"Phone_{filter.PhoneFilter.Trim()}");
            }
            if (filter.FilterTuQuy) activeFilters.Add("TuQuy");
            if (filter.FilterTaxi2) activeFilters.Add("Taxi2");
            if (filter.FilterTaxi3) activeFilters.Add("Taxi3");
            if (filter.FilterTaxi4) activeFilters.Add("Taxi4");
            if (filter.FilterTaxi5) activeFilters.Add("Taxi5");
            if (filter.FilterTaxiDu2) activeFilters.Add("TaxiDu2");
            if (filter.FilterTaxiDu3) activeFilters.Add("TaxiDu3");
            if (filter.FilterDuoiTien) activeFilters.Add("DuoiTien");
            if (filter.FilterSanhGiua) activeFilters.Add("SanhGiua");
            if (filter.FilterTamHoa) activeFilters.Add("TamHoa");
            if (filter.FilterSoiGuong) activeFilters.Add("SoiGuong");
            if (filter.FilterAXA_AYA) activeFilters.Add("AXA_AYA");
            if (filter.FilterAXA_BXB) activeFilters.Add("AXA_BXB");
            if (filter.FilterAXA_BYB) activeFilters.Add("AXA_BYB");
            if (filter.FilterABABAC) activeFilters.Add("ABABAC");
            if (filter.FilterABACAC) activeFilters.Add("ABACAC");
            string filterSuffix = "";
            if (activeFilters.Count > 0)
            {
                if (activeFilters.Count <= 3)
                {
                    filterSuffix = "_" + string.Join("_", activeFilters);
                }
                else
                {
                    filterSuffix = $"_{activeFilters.Count}DieuKien";
                }
            }
            else
            {
                filterSuffix = "_TatCa";
            }

            string rawFileName = $"Xuat_Sim{filterSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                rawFileName = rawFileName.Replace(c, '_');
            }

            return rawFileName;
        }
    }
}
