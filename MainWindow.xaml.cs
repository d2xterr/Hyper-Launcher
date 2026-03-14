using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MinecraftLauncher
{

// Todo : Imma give up on this shite
// Fix Versions Not showing correct data
// Fix ComboBox might replace with panel with scroll bar idk yet
// Remove box used for testing 
// Add Drop in for custom builds
// Fix FullscreenMode Bug were if uncheck dosent load up in fullscreen
// Need to add builder so you can mod and build without leaving launcher
// Rework on the custom ui very choppy and crashes the project

    public partial class MainWindow : Window
    {
        private const string BuildsFolder = "Builds";
        private const string MinecraftExecutable = "minecraft.client";
        private const string StableBuildPrefix = "Stable Build ";
        private const string UpdateUrl = "https://github.com/smartcmd/MinecraftConsoles/releases/download/nightly/LCEWindows64.zip";
        private const string PlaytimeFile = "playtime.txt";

        private string currentUsername = "Player";
        private WebClient webClient;
        private string currentDownloadPath;
        private bool isAutoUpdating = false;
        private List<BuildVersion> allVersions = new List<BuildVersion>();
        private Dictionary<string, TimeSpan> playTimes = new Dictionary<string, TimeSpan>();
        private BuildVersion selectedVersion = null;

        public class BuildVersion
        {
            public string DisplayName { get; set; }
            public string FolderName { get; set; }
            public double VersionNumber { get; set; }
            public DateTime InstallDate { get; set; }
            public string FullPath { get; set; }
            public string Type { get; set; }
            public string Icon { get; set; }
            public TimeSpan PlayTime { get; set; }


            public string PlayTimeDisplay { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();


            Directory.CreateDirectory(BuildsFolder);

            LoadPlayTimes();

            LoadImages();

            LoadSettings();

            LoadInstalledVersions();

            webClient = new WebClient();
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
        }

        private void LoadImages()
        {
            try
            {
                string bannerBgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "banner.jpg");
                if (File.Exists(bannerBgPath))
                {
                    var uri = new Uri(bannerBgPath, UriKind.Absolute);
                    BannerBackgroundImage.Source = new BitmapImage(uri);
                }

                string blockPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "block.png");
                if (File.Exists(blockPath))
                {
                    var uri = new Uri(blockPath, UriKind.Absolute);
                    SidebarBlockImage.Source = new BitmapImage(uri);
// Dont Delete tester object BannerBlockImage.Source = new BitmapImage(uri);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading images: {ex.Message}");
            }
        }

        private void LoadPlayTimes()
        {
            try
            {
                if (File.Exists(PlaytimeFile))
                {
                    string[] lines = File.ReadAllLines(PlaytimeFile);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string folderName = parts[0];
                            if (TimeSpan.TryParse(parts[1], out TimeSpan time))
                            {
                                playTimes[folderName] = time;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void SavePlayTimes()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (var kvp in playTimes)
                {
                    lines.Add($"{kvp.Key}={kvp.Value}");
                }
                File.WriteAllLines(PlaytimeFile, lines);
            }
            catch { }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists("config.txt"))
                {
                    string[] lines = File.ReadAllLines("config.txt");
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Username="))
                        {
                            currentUsername = line.Substring(9);
                            HomeUsernameDisplay.Text = currentUsername;
                            SettingsUsernameTextBox.Text = currentUsername;
                        }
                        else if (line.StartsWith("Fullscreen="))
                        {
                            bool fullscreen = line.Substring(11).ToLower() == "true";
                            SettingsFullscreenCheckBox.IsChecked = fullscreen;
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add($"Username={currentUsername}");
                lines.Add($"Fullscreen={SettingsFullscreenCheckBox.IsChecked}");
                File.WriteAllLines("config.txt", lines);
            }
            catch { }
        }

        private string FormatPlayTime(TimeSpan time)
        {
            if (time.TotalHours < 1)
                return $"{time.Minutes} min";
            else if (time.TotalHours < 24)
                return $"{time.TotalHours:F1} hours";
            else
                return $"{time.TotalDays:F1} days";
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            webClient?.Dispose();
            Application.Current.Shutdown();
        }

        private void LoadInstalledVersions()
        {
            allVersions.Clear();
            string buildsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BuildsFolder);
            int stableCount = 0;
            int customCount = 0;

            if (Directory.Exists(buildsPath))
            {
                var directories = Directory.GetDirectories(buildsPath);
                foreach (var dir in directories)
                {
                    string dirName = Path.GetFileName(dir);

                    if (dirName.StartsWith(StableBuildPrefix))
                    {
                        // Stable is the way to go
                        string versionNumStr = dirName.Replace(StableBuildPrefix, "");
                        if (double.TryParse(versionNumStr, out double versionNum))
                        {
                            TimeSpan playTime = playTimes.ContainsKey(dirName) ? playTimes[dirName] : TimeSpan.Zero;

                            allVersions.Add(new BuildVersion
                            {
                                DisplayName = $"Stable Build {versionNum:F1}",
                                FolderName = dirName,
                                VersionNumber = versionNum,
                                InstallDate = Directory.GetCreationTime(dir),
                                FullPath = dir,
                                Type = "Stable",
                                Icon = "🔄",
                                PlayTime = playTime,
                                PlayTimeDisplay = FormatPlayTime(playTime)
                            });
                            stableCount++;
                        }
                    }
                    else
                    {
                        // Fuck this shitty ass custom build 
                        TimeSpan playTime = playTimes.ContainsKey(dirName) ? playTimes[dirName] : TimeSpan.Zero;

                        allVersions.Add(new BuildVersion
                        {
                            DisplayName = dirName,
                            FolderName = dirName,
                            VersionNumber = 0,
                            InstallDate = Directory.GetCreationTime(dir),
                            FullPath = dir,
                            Type = "Custom",
                            Icon = "📦",
                            PlayTime = playTime,
                            PlayTimeDisplay = FormatPlayTime(playTime)
                        });
                        customCount++;
                    }
                }
            }

            // Version sorted should work hopefully
            allVersions.Sort((a, b) =>
            {
                if (a.Type == "Stable" && b.Type == "Stable")
                    return b.VersionNumber.CompareTo(a.VersionNumber);
                if (a.Type == "Stable" && b.Type != "Stable")
                    return -1;
                if (a.Type != "Stable" && b.Type == "Stable")
                    return 1;
                return string.Compare(a.DisplayName, b.DisplayName);
            });

            // Lowk should update home list 0 clue if it works tho
            HomeVersionListBox.ItemsSource = null;
            HomeVersionListBox.ItemsSource = allVersions;

            if (allVersions.Count > 0)
            {
                HomeVersionListBox.SelectedIndex = 0;
                selectedVersion = allVersions[0];
                SelectedVersionText.Text = $"Selected: {selectedVersion.DisplayName}";
            }
            else
            {
                SelectedVersionText.Text = "No versions available";
            }

            StableBuildsCount.Text = stableCount.ToString();
            CustomBuildsCount.Text = customCount.ToString();

            // Stolen straight from my old project
            BuildVersion latestStable = null;
            foreach (var v in allVersions)
            {
                if (v.Type == "Stable")
                {
                    latestStable = v;
                    break;
                }
            }

            AutoUpdateStatus.Text = latestStable != null ? $"{latestStable.DisplayName} is installed" : "No stable builds installed";

            VersionsItemsControl.ItemsSource = null;
            VersionsItemsControl.ItemsSource = allVersions;
            NoVersionsText.Visibility = allVersions.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void NavigateToHome(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(HomeButton);
            HomePage.Visibility = Visibility.Visible;
            VersionsPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Collapsed;
            PageTitle.Text = "HOME";
            LoadInstalledVersions();
        }

        private void NavigateToVersions(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(VersionsButton);
            HomePage.Visibility = Visibility.Collapsed;
            VersionsPage.Visibility = Visibility.Visible;
            SettingsPage.Visibility = Visibility.Collapsed;
            PageTitle.Text = "VERSIONS";
            LoadInstalledVersions();
        }

        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(SettingsButton);
            HomePage.Visibility = Visibility.Collapsed;
            VersionsPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Visible;
            PageTitle.Text = "SETTINGS";
        }

        private void SetSelectedButton(Button selectedButton)
        {
            HomeButton.Tag = null;
            VersionsButton.Tag = null;
            SettingsButton.Tag = null;
            selectedButton.Tag = "Selected";
        }

        private void HomeVersionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HomeVersionListBox.SelectedItem != null)
            {
                selectedVersion = (BuildVersion)HomeVersionListBox.SelectedItem;
                SelectedVersionText.Text = $"Selected: {selectedVersion.DisplayName}";
            }
        }

        private void HomePlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedVersion != null)
            {
                bool fullscreen = SettingsFullscreenCheckBox.IsChecked == true;

               // Starts timer to track playtime
                DateTime startTime = DateTime.Now;

                // barley works will have to update sooner or later
                LaunchMinecraft(selectedVersion.FolderName, fullscreen, () =>
                {
                    TimeSpan elapsed = DateTime.Now - startTime;
                    if (playTimes.ContainsKey(selectedVersion.FolderName))
                        playTimes[selectedVersion.FolderName] += elapsed;
                    else
                        playTimes[selectedVersion.FolderName] = elapsed;

                    SavePlayTimes();
                    LoadInstalledVersions();
                });
            }
            else
            {
                MessageBox.Show("Please select a version to play.", "No Version Selected",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LaunchVersion_Click(object sender, RoutedEventArgs e)
        {
            // FUCK THE COMBO BOX YEA  FUCK IT
            Button button = sender as Button;
            string versionFolder = button?.Tag?.ToString();
            if (!string.IsNullOrEmpty(versionFolder))
            {
                bool fullscreen = SettingsFullscreenCheckBox.IsChecked == true;

               
                DateTime startTime = DateTime.Now;

            
                LaunchMinecraft(versionFolder, fullscreen, () =>
                {
                    TimeSpan elapsed = DateTime.Now - startTime;
                    if (playTimes.ContainsKey(versionFolder))
                        playTimes[versionFolder] += elapsed;
                    else
                        playTimes[versionFolder] = elapsed;

                    SavePlayTimes();
                    LoadInstalledVersions(); 
                });
            }
        }

        private void LaunchMinecraft(string versionFolder, bool fullscreen, Action onComplete = null)
        {
            try
            {
                string username = currentUsername;
                if (string.IsNullOrWhiteSpace(username))
                    username = "Player";

                string minecraftPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                   BuildsFolder,
                                                   versionFolder,
                                                   MinecraftExecutable);

                if (!File.Exists(minecraftPath))
                {
                    string versionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                     BuildsFolder,
                                                     versionFolder);

                    if (Directory.Exists(versionPath))
                    {
                        var clientFiles = Directory.GetFiles(versionPath, "minecraft.client*");
                        if (clientFiles.Length > 0)
                        {
                            minecraftPath = clientFiles[0];
                        }
                        else
                        {
                            var exeFiles = Directory.GetFiles(versionPath, "*.exe");
                            if (exeFiles.Length > 0)
                            {
                                minecraftPath = exeFiles[0];
                            }
                            else
                            {
                                MessageBox.Show($"No executable found in {versionFolder}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                onComplete?.Invoke();
                                return;
                            }
                        }
                    }
                }

                string arguments = $"-name \"{username}\"";

                if (fullscreen)
                {
                    arguments += " -fullscreen";
                }

                Process process = new Process();
                process.StartInfo.FileName = minecraftPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(minecraftPath);

                process.Exited += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        onComplete?.Invoke();
                    });
                };
                process.EnableRaisingEvents = true;

                process.Start();

                if (CloseAfterLaunchCheckBox.IsChecked == true)
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                onComplete?.Invoke();
            }
        }

        private void DeleteVersion_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string versionFolder = button?.Tag?.ToString();

            if (string.IsNullOrEmpty(versionFolder)) return;

            var result = MessageBox.Show($"Delete {versionFolder}?\nThis will also delete all playtime data.", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string versionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BuildsFolder, versionFolder);
                    if (Directory.Exists(versionPath))
                    {
                        Directory.Delete(versionPath, true);

                        if (playTimes.ContainsKey(versionFolder))
                        {
                            playTimes.Remove(versionFolder);
                            SavePlayTimes();
                        }

                        LoadInstalledVersions();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenBuildsFolder_Click(object sender, RoutedEventArgs e)
        {
            string buildsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BuildsFolder);
            Process.Start("explorer.exe", buildsPath);
        }

        private void SaveUsername_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SettingsUsernameTextBox.Text))
            {
                currentUsername = SettingsUsernameTextBox.Text;
                HomeUsernameDisplay.Text = currentUsername;
                SaveSettings();
                MessageBox.Show("Username saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RememberUsername_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void FullscreenSetting_Changed(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        // working drag and drop i hope
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            MainBorder.Background = (SolidColorBrush)FindResource("BackgroundDark");

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                _ = ProcessDroppedFiles(files);
            }
            e.Handled = true;
        }

        private void CustomBuildsDropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                CustomBuildsDropZone.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
                CustomBuildsDropZone.BorderBrush = (SolidColorBrush)FindResource("AccentSecondary");
                CustomBuildsDropZone.BorderThickness = new Thickness(2);
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void CustomBuildsDropZone_DragLeave(object sender, DragEventArgs e)
        {
            CustomBuildsDropZone.Background = (SolidColorBrush)FindResource("BackgroundMedium");
            CustomBuildsDropZone.BorderBrush = (SolidColorBrush)FindResource("BorderColor");
            CustomBuildsDropZone.BorderThickness = new Thickness(1);
            e.Handled = true;
        }

        private async void CustomBuildsDropZone_Drop(object sender, DragEventArgs e)
        {
            CustomBuildsDropZone.Background = (SolidColorBrush)FindResource("BackgroundMedium");
            CustomBuildsDropZone.BorderBrush = (SolidColorBrush)FindResource("BorderColor");
            CustomBuildsDropZone.BorderThickness = new Thickness(1);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                await ProcessDroppedFiles(files);
            }
            e.Handled = true;
        }

        private async Task ProcessDroppedFiles(string[] files)
        {
            int successCount = 0;
            int failCount = 0;

            CustomProgressSection.Visibility = Visibility.Visible;
            CustomProgressStatusText.Text = "Processing files...";
            CustomProgressBar.IsIndeterminate = true;

            await Task.Run(() =>
            {
                foreach (string file in files)
                {
                    try
                    {
                        if (File.Exists(file) && Path.GetExtension(file).ToLower() == ".zip")
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file);
                            string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BuildsFolder, fileName);

                            int counter = 1;
                            while (Directory.Exists(extractPath))
                            {
                                extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BuildsFolder, $"{fileName} ({counter})");
                                counter++;
                            }

                            Directory.CreateDirectory(extractPath);
                            ZipFile.ExtractToDirectory(file, extractPath, true);
                            successCount++;
                        }
                        else if (Directory.Exists(file))
                        {
                            string dirName = new DirectoryInfo(file).Name;
                            string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BuildsFolder, dirName);

                            int counter = 1;
                            while (Directory.Exists(destPath))
                            {
                                destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BuildsFolder, $"{dirName} ({counter})");
                                counter++;
                            }

                            CopyDirectory(file, destPath);
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                    catch
                    {
                        failCount++;
                    }
                }
            });

            CustomProgressSection.Visibility = Visibility.Collapsed;

            Dispatcher.Invoke(() =>
            {
                LoadInstalledVersions();

                if (successCount > 0)
                {
                    MessageBox.Show($"Successfully installed {successCount} custom build(s).",
                        "Install Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (failCount > 0)
                {
                    MessageBox.Show($"Failed to install {failCount} file(s). Only ZIP files and folders are supported.",
                        "Install Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, dest);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dest = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, dest);
            }
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdates(true);
        }

        private async Task CheckForUpdates(bool showNoUpdateMessage)
        {
            if (isAutoUpdating)
            {
                if (showNoUpdateMessage)
                    MessageBox.Show("Update already in progress.", "Please Wait", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                double latestVersion = GetLatestStableVersion();

                if (latestVersion == 0)
                {
                    if (showNoUpdateMessage)
                    {
                        var result = MessageBox.Show("No stable builds found. Install Stable Build 1.0?",
                            "First Time Setup", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            await DownloadAndInstallStableBuild(1.0);
                        }
                    }
                }
                else
                {
                    double nextVersion = latestVersion + 1.0;

                    if (showNoUpdateMessage)
                    {
                        var result = MessageBox.Show($"Current: Stable Build {latestVersion:F1}\nInstall Stable Build {nextVersion:F1}?",
                            "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            await DownloadAndInstallStableBuild(nextVersion);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (showNoUpdateMessage)
                    MessageBox.Show($"Error checking for updates: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double GetLatestStableVersion()
        {
            double latest = 0;
            foreach (var v in allVersions)
            {
                if (v.Type == "Stable" && v.VersionNumber > latest)
                {
                    latest = v.VersionNumber;
                }
            }
            return latest;
        }

        private async void ForceUpdate_Click(object sender, RoutedEventArgs e)
        {
            double latestVersion = GetLatestStableVersion();
            double nextVersion = latestVersion + 1.0;

            var result = MessageBox.Show($"Install Stable Build {nextVersion:F1}?",
                "Install Latest", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await DownloadAndInstallStableBuild(nextVersion);
            }
        }

        private async Task DownloadAndInstallStableBuild(double version)
        {
            if (isAutoUpdating) return;

            isAutoUpdating = true;

            try
            {
                string versionName = $"{StableBuildPrefix}{version:F1}";
                string versionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BuildsFolder, versionName);

                if (Directory.Exists(versionPath))
                {
                    Directory.Delete(versionPath, true);
                }

                AutoUpdateProgressSection.Visibility = Visibility.Visible;
                AutoUpdateProgressBar.Value = 0;
                AutoUpdateStatusText.Text = $"Downloading Stable Build {version:F1}...";

                currentDownloadPath = Path.GetTempFileName();
                await webClient.DownloadFileTaskAsync(new Uri(UpdateUrl), currentDownloadPath);
            }
            catch (Exception ex)
            {
                AutoUpdateProgressSection.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Download failed: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                isAutoUpdating = false;
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int percentage = e.ProgressPercentage;
                AutoUpdateProgressBar.Value = percentage;
                AutoUpdatePercentageText.Text = $"{percentage}%";
                AutoUpdateStatusText.Text = $"Downloading... {FormatBytes(e.BytesReceived)} / {FormatBytes(e.TotalBytesToReceive)}";
            });
        }

        private async void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                if (e.Error != null)
                {
                    AutoUpdateProgressSection.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Download failed: {e.Error.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    isAutoUpdating = false;
                    return;
                }

                if (e.Cancelled)
                {
                    AutoUpdateProgressSection.Visibility = Visibility.Collapsed;
                    isAutoUpdating = false;
                    return;
                }

                try
                {
                    double version = GetLatestStableVersion();
                    if (version == 0) version = 1.0;

                    string versionDisplay = $"{StableBuildPrefix}{version:F1}";
                    string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BuildsFolder, versionDisplay);

                    AutoUpdateStatusText.Text = "Extracting...";

                    Directory.CreateDirectory(extractPath);
                    await Task.Run(() => ZipFile.ExtractToDirectory(currentDownloadPath, extractPath, true));

                    File.Delete(currentDownloadPath);

                    AutoUpdateStatusText.Text = "Complete!";
                    AutoUpdateProgressBar.Value = 100;

                    await Task.Delay(1500);
                    AutoUpdateProgressSection.Visibility = Visibility.Collapsed;

                    LoadInstalledVersions();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Extraction failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    AutoUpdateProgressSection.Visibility = Visibility.Collapsed;
                }
                finally
                {
                    isAutoUpdating = false;
                }
            });
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        protected override void OnClosed(EventArgs e)
        {
            webClient?.Dispose();
            base.OnClosed(e);
        }
    }
}