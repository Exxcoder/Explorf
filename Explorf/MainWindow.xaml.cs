using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Explorf
{
    public class FolderModel
    {
        public bool IsSelected { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        public ArrayList FolderList { get; set; } = new ArrayList();

        private WindowStateConfig _windowState = new WindowStateConfig();

        // ---------- Win32 API для работы с окнами Explorer ----------
        private const uint WM_COMMAND = 0x0111;
        private const int ID_NEW_TAB = 0x0000A21B; // Системная команда создания вкладки в Explorer Windows 11
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public MainWindow()
        {
            InitializeComponent();

            this.Icon = new BitmapImage(new Uri("pack://application:,,,/appoh.ico"));

            FolderListView.ItemsSource = FolderList;

            Loaded += Window_Loaded;
            Closing += Window_Closing;
        }

        // ---------- Жизненный цикл окна ----------
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _windowState = WindowStateConfig.Load();
            RestoreWindowState();

            if (_windowState.Folders.Count > 0)
            {
                foreach (var f in _windowState.Folders)
                {
                    FolderList.Add(new FolderModel
                    {
                        Name = f.Name,
                        Path = f.Path,
                        IsSelected = f.IsSelected
                    });
                }
            }
            else
            {
                FolderList.Add(new FolderModel { Name = "Проекты C#", Path = @"C:\Dev\Projects" });
                FolderList.Add(new FolderModel { Name = "Загрузки", Path = @"C:\Users\Public\Downloads" });
            }
            FolderListView.Items.Refresh();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveWindowState();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); }
                catch (InvalidOperationException) { }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        // ---------- Сохранение / восстановление окна ----------
        private void RestoreWindowState()
        {
            if (_windowState.Width >= MinWidth) Width = _windowState.Width;
            if (_windowState.Height >= MinHeight) Height = _windowState.Height;

            if (!double.IsNaN(_windowState.Left) && !double.IsNaN(_windowState.Top))
            {
                double left = _windowState.Left;
                double top = _windowState.Top;

                double vLeft = SystemParameters.VirtualScreenLeft;
                double vTop = SystemParameters.VirtualScreenTop;
                double vRight = vLeft + SystemParameters.VirtualScreenWidth;
                double vBottom = vTop + SystemParameters.VirtualScreenHeight;

                bool onScreen =
                    left + Width > vLeft + 40 &&
                    top + Height > vTop + 40 &&
                    left < vRight - 40 &&
                    top < vBottom - 40;

                if (onScreen)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Left = left;
                    Top = top;
                }
            }

            if (_windowState.IsMaximized)
                WindowState = WindowState.Maximized;
        }

        private void SaveWindowState()
        {
            if (WindowState == WindowState.Maximized)
            {
                _windowState.IsMaximized = true;
                var rb = RestoreBounds;
                _windowState.Left = rb.Left;
                _windowState.Top = rb.Top;
                _windowState.Width = rb.Width;
                _windowState.Height = rb.Height;
            }
            else
            {
                _windowState.IsMaximized = false;
                _windowState.Left = Left;
                _windowState.Top = Top;
                _windowState.Width = Width;
                _windowState.Height = Height;
            }

            SaveFoldersOnly();
            _windowState.Save();
        }

        private void SaveFoldersOnly()
        {
            _windowState.Folders.Clear();
            foreach (FolderModel f in FolderList)
            {
                if (f == null) continue;
                _windowState.Folders.Add(new FolderEntry
                {
                    Name = f.Name,
                    Path = f.Path,
                    IsSelected = f.IsSelected
                });
            }
        }

        private void SaveFoldersOnlyAndSave()
        {
            SaveFoldersOnly();
            _windowState.Save();
        }

        // ---------- Добавление: буфер обмена ----------
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show("Буфер обмена пуст. Скопируйте путь к папке и повторите.",
                                "Explorf", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var raw = Clipboard.GetText();

            var lines = raw
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
            {
                MessageBox.Show("В буфере нет текста с путём.",
                                "Explorf", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int added = 0;
            foreach (var line in lines)
            {
                string path = line.Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(path) && AddFolderByPath(path))
                    added++;
            }

            if (added == 0)
                MessageBox.Show("Ничего не добавлено: путь не найден или уже в списке.",
                                "Explorf", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                SaveFoldersOnlyAndSave();
        }

        // ---------- Добавление: диалог выбора папки ----------
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Выберите папку для добавления",
                ShowNewFolderButton = true
            })
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK
                    && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    if (AddFolderByPath(dialog.SelectedPath))
                        SaveFoldersOnlyAndSave();
                }
            }
        }

        // ---------- Общая логика добавления ----------
        private bool AddFolderByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            path = path.Trim().Trim('"');

            if (!Directory.Exists(path)) return false;

            foreach (FolderModel item in FolderList)
            {
                if (item != null && string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            string name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            if (string.IsNullOrEmpty(name)) name = path;

            FolderList.Add(new FolderModel { Name = name, Path = path });
            FolderListView.Items.Refresh();
            return true;
        }

        // ---------- Удаление ----------
        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var toRemove = new ArrayList();
            foreach (FolderModel f in FolderList)
            {
                if (f != null && f.IsSelected)
                    toRemove.Add(f);
            }

            foreach (var item in toRemove)
                FolderList.Remove(item);

            FolderListView.Items.Refresh();
            SaveFoldersOnlyAndSave();
        }

        // ---------- Открытие папок (в одном новом окне с вкладками для Windows 11) ----------
        private async void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null) button.IsEnabled = false;

            try
            {
                await OpenFoldersAsync();
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        private async Task OpenFoldersAsync()
        {
            var selectedFolders = new ArrayList();
            foreach (FolderModel f in FolderList)
            {
                if (f != null && f.IsSelected && !string.IsNullOrWhiteSpace(f.Path) && Directory.Exists(f.Path))
                {
                    string fullPath = System.IO.Path.GetFullPath(f.Path);
                    bool alreadyAdded = false;
                    foreach (string existing in selectedFolders)
                    {
                        if (string.Equals(existing, fullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }

                    if (!alreadyAdded)
                    {
                        selectedFolders.Add(fullPath);
                    }
                }
            }

            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Нет отмеченных существующих папок для открытия.",
                                "Explorf", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string[] foldersToOpen = (string[])selectedFolders.ToArray(typeof(string));

            // Если выбрана всего 1 папка — обычное открытие
            if (foldersToOpen.Length == 1)
            {
                Process.Start("explorer.exe", "\"" + foldersToOpen[0] + "\"");
                return;
            }

            // Несколько папок: открываем в одном новом окне с вкладками
            await OpenInTabbedWindowAsync(foldersToOpen);
        }

        private async Task OpenInTabbedWindowAsync(string[] folders)
        {
            // 1. Получаем список всех уже открытых окон Проводника (CabinetWClass),
            // чтобы отличить существующие окна (например, окно "Загрузки") от нового
            ArrayList beforeWindows = GetAllExplorerWindows();

            // 2. Открываем первое окно с первой выбранной папкой
            Process.Start("explorer.exe", "\"" + folders[0] + "\"");

            // 3. Ждем появления дескриптора НОВОГО окна Проводника
            IntPtr targetHwnd = IntPtr.Zero;
            for (int i = 0; i < 40; i++) // До 4 секунд ожидания
            {
                await Task.Delay(100);
                ArrayList currentWindows = GetAllExplorerWindows();
                for (int c = 0; c < currentWindows.Count; c++)
                {
                    IntPtr candidate = (IntPtr)currentWindows[c];
                    if (!beforeWindows.Contains(candidate))
                    {
                        targetHwnd = candidate;
                        break;
                    }
                }

                if (targetHwnd != IntPtr.Zero)
                    break;
            }

            // Если дескриптор нового окна не успел определиться в списке, берем последнее найденное окно
            if (targetHwnd == IntPtr.Zero)
            {
                ArrayList current = GetAllExplorerWindows();
                if (current.Count > 0)
                {
                    targetHwnd = (IntPtr)current[current.Count - 1];
                }
            }

            if (targetHwnd == IntPtr.Zero) return;

            ShowWindow(targetHwnd, SW_RESTORE);
            SetForegroundWindow(targetHwnd);
            await Task.Delay(500);

            // 4. Подключаем Shell.Application для навигации вкладок через COM
            Type shellType = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = (shellType != null) ? Activator.CreateInstance(shellType) : null;

            // 5. Последовательно создаем вкладки для всех остальных папок
            for (int k = 1; k < folders.Length; k++)
            {
                string targetFolder = folders[k];

                // Фиксируем COM-идентификаторы вкладок до создания новой
                ArrayList beforeTabs = (shell != null) ? GetTabsForWindow(shell.Windows(), targetHwnd.ToInt64()) : new ArrayList();
                ArrayList beforeIds = new ArrayList();
                for (int b = 0; b < beforeTabs.Count; b++)
                {
                    IntPtr unk = GetComIdentity(beforeTabs[b]);
                    if (unk != IntPtr.Zero) beforeIds.Add(unk);
                }

                // Создаем новую вкладку через системную команду 0xA21B
                PostMessage(targetHwnd, WM_COMMAND, (IntPtr)ID_NEW_TAB, IntPtr.Zero);

                // Ищем созданную вкладку в Shell.Application
                dynamic newTab = null;
                if (shell != null)
                {
                    for (int attempt = 0; attempt < 30; attempt++) // До 3 секунд
                    {
                        await Task.Delay(100);
                        ArrayList currentTabs = GetTabsForWindow(shell.Windows(), targetHwnd.ToInt64());

                        for (int cur = 0; cur < currentTabs.Count; cur++)
                        {
                            IntPtr unk = GetComIdentity(currentTabs[cur]);
                            if (unk != IntPtr.Zero && !beforeIds.Contains(unk))
                            {
                                newTab = currentTabs[cur];
                                break;
                            }
                        }

                        if (newTab == null && currentTabs.Count > beforeTabs.Count)
                        {
                            newTab = currentTabs[currentTabs.Count - 1];
                        }

                        if (newTab != null) break;
                    }
                }

                // Выполняем навигацию вкладки по прямому вызову COM
                if (newTab != null)
                {
                    NavigateTab(newTab, targetFolder);
                    await Task.Delay(200);
                }
            }

            ShowWindow(targetHwnd, SW_RESTORE);
            SetForegroundWindow(targetHwnd);
        }

        // ---------- Вспомогательные методы Win32 / COM ----------
        private static ArrayList GetAllExplorerWindows()
        {
            var list = new ArrayList();
            IntPtr h = IntPtr.Zero;
            while ((h = FindWindowEx(IntPtr.Zero, h, "CabinetWClass", null)) != IntPtr.Zero)
            {
                list.Add(h);
            }
            return list;
        }

        private static ArrayList GetTabsForWindow(dynamic shellWindows, long targetHwnd)
        {
            var list = new ArrayList();
            if (shellWindows == null) return list;

            try
            {
                int count = shellWindows.Count;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        dynamic w = shellWindows.Item(i);
                        if (w != null && Convert.ToInt64(w.HWND) == targetHwnd)
                        {
                            list.Add(w);
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return list;
        }

        private static IntPtr GetComIdentity(object comObj)
        {
            if (comObj == null) return IntPtr.Zero;
            try
            {
                IntPtr pUnk = Marshal.GetIUnknownForObject(comObj);
                Marshal.Release(pUnk);
                return pUnk;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static void NavigateTab(dynamic tab, string path)
        {
            try
            {
                tab.Navigate2(path);
            }
            catch
            {
                try
                {
                    tab.Navigate(path);
                }
                catch
                {
                    try
                    {
                        string uri = new Uri(path).AbsoluteUri;
                        tab.Navigate2(uri);
                    }
                    catch { }
                }
            }
        }
    }
}
