using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace Explorf
{
    public class FolderItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsSelected { get; set; }
    }

    public partial class MainWindow : Window
    {
        private readonly string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Explorf",
            "folders.xml"
        );

        public ObservableCollection<FolderItem> Folders { get; set; } = new ObservableCollection<FolderItem>();

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            FolderListView.ItemsSource = Folders;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку для добавления в список";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    string folderName = Path.GetFileName(selectedPath);

                    if (string.IsNullOrEmpty(folderName))
                        folderName = selectedPath;

                    if (!Folders.Any(f => f.Path.Equals(selectedPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        Folders.Add(new FolderItem
                        {
                            Name = folderName,
                            Path = selectedPath,
                            IsSelected = true
                        });
                        SaveSettings();
                    }
                }
            }
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (FolderListView.SelectedItem is FolderItem selectedItem)
            {
                Folders.Remove(selectedItem);
                SaveSettings();
            }
        }

        private async void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            var selectedFolders = Folders.Where(f => f.IsSelected && Directory.Exists(f.Path)).ToList();

            if (selectedFolders.Count > 0)
            {
                // 1. Первая папка — создаёт окно Проводника
                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedFolders[0].Path,
                    UseShellExecute = true
                });

                // 2. Пауза, чтобы Windows зарегистрировала окно
                await Task.Delay(800);

                // 3. Остальные папки
                for (int i = 1; i < selectedFolders.Count; i++)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = selectedFolders[i].Path,
                        UseShellExecute = true
                    });
                    await Task.Delay(300);
                }
            }

            await Task.Delay(500);
            Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var serializer = new XmlSerializer(typeof(List<FolderItem>));
                    using (var stream = new FileStream(_configPath, FileMode.Open))
                    {
                        var items = (List<FolderItem>)serializer.Deserialize(stream);
                        if (items != null)
                        {
                            Folders.Clear();
                            foreach (var item in items)
                            {
                                Folders.Add(item);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ошибка чтения
            }
        }

        private void SaveSettings()
        {
            try
            {
                string directory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var serializer = new XmlSerializer(typeof(List<FolderItem>));
                using (var stream = new FileStream(_configPath, FileMode.Create))
                {
                    serializer.Serialize(stream, Folders.ToList());
                }
            }
            catch
            {
                // Ошибка сохранения
            }
        }
    }
}