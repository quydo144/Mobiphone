using System.IO;
using System.Windows;
using System.Linq;
using ExcelToSQLite.Services;
using ExcelToSQLite.Models;
using System.Collections.Generic;
using WinForms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace ExcelToSQLite
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
        private string? _currentPhoneFilter = null;
        private bool? _currentMiniThanTai = null;
        private bool? _currentBigThanTai = null;
        private bool? _currentLocPhat = null;
        private bool? _currentTuQuy = null;
        private bool? _currentTuQuyGiua = null;
        private bool? _currentTamHoaKep = null;

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
                openFileDialog.Title = "Select an Excel file";

                if (openFileDialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    _selectedFilePath = openFileDialog.FileName;
                    _selectedFolderPath = string.Empty;
                    _isFolder = false;
                    txtFilePath.Text = _selectedFilePath;

                    btnImport.IsEnabled = true;
                    txtStatus.Text = "File selected. Ready to import.";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
        }

        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new WinForms.FolderBrowserDialog())
            {
                folderDialog.Description = "Select a folder containing Excel files";
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
                        txtStatus.Text = $"Folder selected. Found {excelFiles.Length} Excel file(s). Ready to import.";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    }
                    else
                    {
                        txtStatus.Text = "No Excel files found in selected folder.";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    }
                }
            }
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath) && string.IsNullOrEmpty(_selectedFolderPath))
            {
                MessageBox.Show("Please select a file or folder first.", "Warning",
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
                txtStatus.Text = $"Processing: {fileName}...";
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;

                // Read Excel file
                progressBar.Value = 20;
                txtProgress.Text = "Reading Excel file...";
                var records = await Task.Run(() => _excelService.ReadExcelFile(filePath));

                if (records.Count == 0)
                {
                    progressBar.Visibility = Visibility.Collapsed;
                    txtProgress.Visibility = Visibility.Collapsed;
                    MessageBox.Show("No valid phone numbers found in the Excel file.\n\nPhone numbers must be exactly 9 digits.", "Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    txtStatus.Text = "No valid data to import.";
                    return;
                }

                // Import to database
                progressBar.Value = 50;
                txtProgress.Text = $"Importing {records.Count} phone numbers to database...";
                var (importedCount, duplicateCount) = await Task.Run(() => _databaseService.ImportRecords(records));

                // Reload data
                progressBar.Value = 90;
                txtProgress.Text = "Reloading data...";
                LoadDataFromDatabase();

                // Complete
                progressBar.Value = 100;
                txtProgress.Text = "Import completed!";
                await Task.Delay(500); // Show 100% briefly

                progressBar.Visibility = Visibility.Collapsed;
                txtProgress.Visibility = Visibility.Collapsed;

                string statusMessage = $"Successfully imported {importedCount} phone numbers from {fileName}";
                if (duplicateCount > 0)
                {
                    statusMessage += $" ({duplicateCount} duplicates skipped)";
                }
                txtStatus.Text = statusMessage;
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;

                string messageDetails = $"Import completed successfully!\n\n" +
                    $"File: {fileName}\n" +
                    $"Total phone numbers in file: {records.Count}\n" +
                    $"Successfully imported: {importedCount}\n";

                if (duplicateCount > 0)
                {
                    messageDetails += $"Duplicates skipped: {duplicateCount}";
                }

                MessageBox.Show(
                    messageDetails,
                    "Import Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressBar.Visibility = Visibility.Collapsed;
                txtProgress.Visibility = Visibility.Collapsed;
                txtStatus.Text = "Import failed!";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Error importing data: {ex.Message}", "Error",
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
                    MessageBox.Show("No Excel files found in the selected folder.", "Information",
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

                txtStatus.Text = $"Processing {excelFiles.Length} Excel files...";
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;

                foreach (var filePath in excelFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        filesProcessed++;

                        // Update progress
                        progressBar.Value = (filesProcessed * 100.0 / excelFiles.Length);
                        txtProgress.Text = $"Processing file {filesProcessed}/{excelFiles.Length}: {fileName}";

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
                txtProgress.Text = "Reloading data...";
                LoadDataFromDatabase();

                // Complete
                progressBar.Value = 100;
                await Task.Delay(500);

                progressBar.Visibility = Visibility.Collapsed;
                txtProgress.Visibility = Visibility.Collapsed;

                string statusMessage = $"Folder import completed! Imported {totalImported} phone numbers from {filesProcessed} files";
                if (totalDuplicates > 0)
                {
                    statusMessage += $" ({totalDuplicates} duplicates)";
                }
                txtStatus.Text = statusMessage;
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;

                string messageDetails = $"Folder import completed!\n\n" +
                    $"Files processed: {filesProcessed}\n" +
                    $"Total phone numbers read: {totalRecordsRead}\n" +
                    $"Successfully imported: {totalImported}\n" +
                    $"Duplicates skipped: {totalDuplicates}\n";

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
                    "Folder Import Complete",
                    MessageBoxButton.OK,
                    filesWithErrors > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressBar.Visibility = Visibility.Collapsed;
                txtProgress.Visibility = Visibility.Collapsed;
                txtStatus.Text = "Folder import failed!";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Error importing folder: {ex.Message}", "Error",
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

        private void LoadDataFromDatabase(string? phoneFilter = null,
            bool? miniThanTai = null, bool? bigThanTai = null, bool? locPhat = null,
            bool? tuQuy = null, bool? tuQuyGiua = null, bool? tamHoaKep = null)
        {
            // Save current filter state
            _currentPhoneFilter = phoneFilter;
            _currentMiniThanTai = miniThanTai;
            _currentBigThanTai = bigThanTai;
            _currentLocPhat = locPhat;
            _currentTuQuy = tuQuy;
            _currentTuQuyGiua = tuQuyGiua;
            _currentTamHoaKep = tamHoaKep;

            try
            {
                // Get filtered count for pagination
                _filteredRecords = _databaseService.GetRecordCount(
                    phoneFilter ?? "",
                    miniThanTai == true,
                    bigThanTai == true,
                    locPhat == true,
                    tuQuy == true,
                    tuQuyGiua == true,
                    tamHoaKep == true);
                _totalPages = (_filteredRecords + _pageSize - 1) / _pageSize; // Ceiling division

                if (_totalPages == 0) _totalPages = 1;
                if (_currentPage > _totalPages) _currentPage = _totalPages;
                if (_currentPage < 1) _currentPage = 1;

                // Get paginated records with filters
                var records = _databaseService.GetRecordsWithPagination(
                    _currentPage, _pageSize,
                    phoneFilter ?? "",
                    miniThanTai == true,
                    bigThanTai == true,
                    locPhat == true,
                    tuQuy == true,
                    tuQuyGiua == true,
                    tamHoaKep == true);
                dgData.ItemsSource = records;

                // Get total count in database (without filter) for Total Records display
                _totalRecords = _databaseService.GetRecordCount("", false, false, false, false, false, false);

                // Update UI
                txtRecordCount.Text = _totalRecords.ToString();
                txtPageInfo.Text = $"Page {_currentPage} of {_totalPages}";

                // Update button states
                btnFirstPage.IsEnabled = _currentPage > 1;
                btnPrevPage.IsEnabled = _currentPage > 1;
                btnNextPage.IsEnabled = _currentPage < _totalPages;
                btnLastPage.IsEnabled = _currentPage < _totalPages;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFirstPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadDataFromDatabase(_currentPhoneFilter, _currentMiniThanTai, _currentBigThanTai,
                _currentLocPhat, _currentTuQuy, _currentTuQuyGiua, _currentTamHoaKep);
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadDataFromDatabase(_currentPhoneFilter, _currentMiniThanTai, _currentBigThanTai,
                    _currentLocPhat, _currentTuQuy, _currentTuQuyGiua, _currentTamHoaKep);
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadDataFromDatabase(_currentPhoneFilter, _currentMiniThanTai, _currentBigThanTai,
                    _currentLocPhat, _currentTuQuy, _currentTuQuyGiua, _currentTamHoaKep);
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages;
            LoadDataFromDatabase(_currentPhoneFilter, _currentMiniThanTai, _currentBigThanTai,
                _currentLocPhat, _currentTuQuy, _currentTuQuyGiua, _currentTamHoaKep);
        }

        private void CmbPageSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!_isInitialized || cmbPageSize.SelectedItem == null)
                return;

            var selectedItem = (System.Windows.Controls.ComboBoxItem)cmbPageSize.SelectedItem;
            _pageSize = int.Parse(selectedItem.Content.ToString() ?? "100");
            _currentPage = 1; // Reset to first page when page size changes
            LoadDataFromDatabase(_currentPhoneFilter, _currentMiniThanTai, _currentBigThanTai,
                _currentLocPhat, _currentTuQuy, _currentTuQuyGiua, _currentTamHoaKep);
        }

        private void BtnApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Read filter values
                string? phoneFilter = string.IsNullOrWhiteSpace(txtFilterPhone.Text)
                    ? null
                    : txtFilterPhone.Text.Trim();

                bool? miniThanTai = chkFilterMiniThanTai.IsChecked == true ? true : null;
                bool? bigThanTai = chkFilterBigThanTai.IsChecked == true ? true : null;
                bool? locPhat = chkFilterLocPhat.IsChecked == true ? true : null;
                bool? tuQuy = chkFilterTuQuy.IsChecked == true ? true : null;
                bool? tuQuyGiua = chkFilterTuQuyGiua.IsChecked == true ? true : null;
                bool? tamHoaKep = chkFilterTamHoaKep.IsChecked == true ? true : null;

                // Reset to first page when applying filter
                _currentPage = 1;

                // Load data with filters
                LoadDataFromDatabase(phoneFilter, miniThanTai, bigThanTai, locPhat,
                    tuQuy, tuQuyGiua, tamHoaKep);

                txtFilterStatus.Text = $"Filter applied. Found {_filteredRecords} records.";
                txtFilterStatus.Foreground = System.Windows.Media.Brushes.Blue;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filter: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear all filter controls
                txtFilterPhone.Clear();
                chkFilterMiniThanTai.IsChecked = false;
                chkFilterBigThanTai.IsChecked = false;
                chkFilterLocPhat.IsChecked = false;
                chkFilterTuQuy.IsChecked = false;
                chkFilterTuQuyGiua.IsChecked = false;
                chkFilterTamHoaKep.IsChecked = false;

                // Reset to first page
                _currentPage = 1;

                // Load all data without filters
                LoadDataFromDatabase();

                txtFilterStatus.Text = $"Filter cleared. Showing all {_totalRecords} records.";
                txtFilterStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing filter: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
