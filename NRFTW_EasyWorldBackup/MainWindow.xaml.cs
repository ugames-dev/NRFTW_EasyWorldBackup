using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using System.IO;
using System.Threading;
using System.Net;
using System.Web;
using static NRFTW_EasyWorldBacup.MainWindow;

namespace NRFTW_EasyWorldBacup
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {      
      public class WorldInfo
      {
         public string Name { get; set; }
         public string Path { get; set; }
      }

      [Serializable]
      public class Realm
      {
         public string id { get; set; }
         public string accountId { get; set; }
         public string name { get; set; }
         public DateTime? createdAt { get; set; }
         public string updatedAt { get; set; }
         public int revision { get; set; }
         public bool enabled { get; set; }
         public object infoVersion { get; set; }
         public Info info { get; set; }
      }

      [Serializable]
      public class RealmClone : Realm
      {
         public string title;
         public string memo;
      }

      public class Info
      {
         public bool TutorialDone { get; set; }
         public Dictionary<string, string> CharacterLocations { get; set; }
         public object domain { get; set; }
         public bool StartedInEndGame { get; set; }
         public string CharacterId { get; set; }
         public bool OnlineRealm { get; set; }
         public bool PlayPrologue { get; set; }
         public WorldStateData WorldStateData { get; set; }
         public bool CanDoTutorial { get; set; }
      }

      public class WorldStateData
      {
         public bool Sacrament { get; set; }
         public bool WeepingSisters { get; set; }
         public bool OfRatsAndRaiders { get; set; }
         public bool MountainPass { get; set; }
         public bool Act1MainQuests { get; set; }
         public bool EA0SideContent { get; set; }
         public int Typ { get; set; }
         public int BuildID { get; set; }
         public object Character { get; set; }
         public object Account { get; set; }
         public object AccountId { get; set; }
         public object CharacterId { get; set; }
         public object RealmId { get; set; }
         public object SteamId { get; set; }
         public object Platform { get; set; }
         public object PlatformId { get; set; }
         public int CompletedCount { get; set; }
      }

      public ObservableCollection<WorldInfo> clones { get; set; }

      string pathRoot;

      public MainWindow()
      {
         InitializeComponent();

         string localLowFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\..\\" + "LocalLow";
         pathRoot = System.IO.Path.Combine(localLowFolderPath, "UGames\\NoRestForTheWicked_WorldBackupData");

         if (Directory.Exists(pathRoot) == false)
         {
            Directory.CreateDirectory(pathRoot);
         }
      }

      private void Window_Loaded(object sender, RoutedEventArgs e)
      {
         clones = new ObservableCollection<WorldInfo>();
         UpdateWorldList();

         DataContext = this;
      }

      void UpdateWorldList()
      {
         lbWorldList.Items.Clear();

         string localLowFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\..\\" + "LocalLow";

         if (!Directory.Exists(localLowFolderPath)) return;

         string path = Path.Combine(localLowFolderPath, "Moon Studios\\NoRestForTheWicked\\DataStore");
         DirectoryInfo di = new DirectoryInfo(path);

         var sortedFiles = di.GetFiles().OrderByDescending(f => f.CreationTime).ToArray();

         foreach (var file in sortedFiles)
         {
            if (!file.Name.Contains("Realm")) continue;

            var worldInfo = ReadWorldFile(file.FullName);

            if (worldInfo != null)
            {
               ListBoxItem lbi = new ListBoxItem();
               lbi.Content = worldInfo.name;
               lbi.Tag = file.FullName;

               lbWorldList.Items.Add(lbi);
            }
         }
      }

      Realm? ReadWorldFile(string path)
      {
         bool readSuccess = false;
         int tryCount = 0;

         Realm? worldInfo = null;

         while (readSuccess == false && tryCount < 5)
         {
            try
            {
               var json = File.ReadAllText(path);
               worldInfo = JsonConvert.DeserializeObject<Realm>(json);

               readSuccess = true;
            }
            catch
            {
               tryCount++;
               Thread.Sleep(1000);
            }
         }

         return worldInfo;
      }

      private void lbWorldList_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         lbWorldInfo.Items.Clear();
         lbAutoBackup.Items.Clear();
         clones.Clear();

         if (lbWorldList.SelectedItem == null) return;

         var item = lbWorldList.SelectedItem as ListBoxItem;
         var filePath = item.Tag.ToString();
         var worldInfo = ReadWorldFile(filePath);

         if (worldInfo == null) return;

         lbWorldInfo.Items.Add("생성: " + worldInfo.createdAt.ToString());
         lbWorldInfo.Items.Add("업데이트: " + worldInfo.updatedAt);

         if (worldInfo.info.CharacterLocations.Count > 0 && worldInfo.info.CharacterId != null)
         {
            lbWorldInfo.Items.Add("캐릭터 위치: " + worldInfo.info.CharacterLocations[worldInfo.info.CharacterId]);
         }

         var worldFolder = Path.Combine(pathRoot, Path.GetFileNameWithoutExtension(filePath));

         if (Directory.Exists(worldFolder))
         {
            var di = new DirectoryInfo(worldFolder);

            foreach (var file in di.GetFiles())
            {
               var title = file.Name.Substring(0, file.Name.IndexOf("_Realm_"));
               title = HttpUtility.UrlDecode(title);

               clones.Add(new WorldInfo() { Name = title, Path = file.FullName });
            }
         }

         var autoBackupPath = Path.Combine(worldFolder, "LatestBackup");
         autoBackupPath = Path.Combine(autoBackupPath, Path.GetFileName(filePath));

         if (File.Exists(autoBackupPath))
         {
            lbAutoBackup.Items.Add(autoBackupPath);
         }
      }

      private void WorldRefresh_Click(object sender, RoutedEventArgs e)
      {
         UpdateWorldList();
      }

      private void MakeClone_Click(object sender, RoutedEventArgs e)
      {
         if (lbWorldList.SelectedItem == null) return;
         var item = lbWorldList.SelectedItem as ListBoxItem;

         var fileName = Path.GetFileNameWithoutExtension(item.Tag.ToString());
         var worldFolder = Path.Combine(pathRoot, fileName);

         if (Directory.Exists(worldFolder) == false)
         {
            Directory.CreateDirectory(worldFolder);
         }

         string title = HttpUtility.UrlEncode("N/A");

         var di = new DirectoryInfo(worldFolder);
         var cloneName = title + "_" + fileName + "_" + (di.GetFiles().Count() + 1).ToString() + ".dat";

         var dst = Path.Combine(worldFolder, cloneName);
         File.Copy(item.Tag.ToString(), dst);

         clones.Add(new WorldInfo() { Name = "N/A", Path = dst });
      }

      private void list_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
      {
         if (e.Column.DisplayIndex == 0)
         {
            var newVal = ((TextBox)e.EditingElement).Text;

            if (newVal.Length > 40)
            {
               MessageBox.Show("글자 길이는 최대 40글자 입니다.");
               e.Cancel = true;
               return;
            }

            var item = (WorldInfo)list.SelectedItem;

            if (newVal != item.Name)
            {
               var path = Path.GetDirectoryName(item.Path);
               var fileName = Path.GetFileName(item.Path);

               int idx = fileName.IndexOf("_Realm_");
               var newFileName = HttpUtility.UrlEncode(newVal) + fileName.Substring(idx);

               var dst = Path.Combine(path, newFileName);
               File.Move(item.Path, dst);
               item.Path = dst;
            }
         }
      }

      private void list_MouseDoubleClick(object sender, MouseButtonEventArgs e)
      {
         var item = list.SelectedItem as WorldInfo;
         var res = MessageBox.Show("선택한 백업으로 세계를 변경 하시겠습니까?\r\n*게임이 종료 됐거나 메인 메뉴에 있을 때 진행해 주세요*\r\n*현재 플레이하던 세계는 자동으로 백업됩니다.*", "백업 복원", MessageBoxButton.OKCancel);

         if (res == MessageBoxResult.Cancel) return;

         var world = lbWorldList.SelectedItem as ListBoxItem;
         var src = world.Tag.ToString();
         var worldFolder = Path.Combine(pathRoot, Path.GetFileNameWithoutExtension(src));
         var latestWorldBackupFolder = Path.Combine(worldFolder, "LatestBackup");

         if (Directory.Exists(latestWorldBackupFolder) == false)
         {
            Directory.CreateDirectory(latestWorldBackupFolder);
         }

         try
         {
            File.Copy(src, Path.Combine(latestWorldBackupFolder, Path.GetFileName(src)), true);
         }
         catch { }
         
         try
         {
            File.Copy(item.Path, src, true);
            MessageBox.Show("복원에 성공했습니다.");
         }
         catch
         {
            MessageBox.Show("복원에 실패했습니다.");
         }
      }

      private void lbAutoBackup_MouseDoubleClick(object sender, MouseButtonEventArgs e)
      {
         var res = MessageBox.Show("선택한 백업으로 세계를 변경 하시겠습니까?\r\n*게임이 종료 됐거나 메인 메뉴에 있을 때 진행해 주세요", "백업 복원", MessageBoxButton.OKCancel);

         if (res == MessageBoxResult.Cancel) return;

         var world = lbWorldList.SelectedItem as ListBoxItem;
         var src = world.Tag.ToString();
         var worldFolder = Path.Combine(pathRoot, Path.GetFileNameWithoutExtension(src));

         try
         {
            File.Copy(lbAutoBackup.SelectedItem.ToString(), src, true);
            MessageBox.Show("복원에 성공했습니다.");
         }
         catch
         {
            MessageBox.Show("복원에 실패했습니다.");
         }
      }

      private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         lbCloneWroldInfo.Items.Clear();         

         if (list.SelectedItem == null)
         {
            DeleteBackup.Visibility = Visibility.Hidden;
            return;
         }

         var item = list.SelectedItem as WorldInfo;

         if (ShowWorldInfo(item.Path)) DeleteBackup.Visibility = Visibility.Visible;
      }

      bool ShowWorldInfo(string path)
      {
         var worldInfo = ReadWorldFile(path);

         if (worldInfo == null) return false;

         FileInfo fi = new FileInfo(path);

         lbCloneWroldInfo.Items.Add("백업 생성 시간: ");
         lbCloneWroldInfo.Items.Add(fi.CreationTime.ToString());

         if (worldInfo.info.CharacterLocations.Count > 0 && worldInfo.info.CharacterId != null)
         {
            lbCloneWroldInfo.Items.Add(" ");
            lbCloneWroldInfo.Items.Add("캐릭터 위치: " + worldInfo.info.CharacterLocations[worldInfo.info.CharacterId]);
         }

         return true;
      }

      private void lbCloneWroldInfo_PreviewMouseDown(object sender, MouseButtonEventArgs e)
      {
         e.Handled = true;
      }

      private void lbWorldInfo_PreviewMouseDown(object sender, MouseButtonEventArgs e)
      {
         e.Handled = true;
      }

      private void DeleteBackup_Click(object sender, RoutedEventArgs e)
      {
         var item = list.SelectedItem as WorldInfo;

         var res = MessageBox.Show("선택한 백업을 삭제 하시겠습니까?", "백업 삭제", MessageBoxButton.OKCancel);

         if (res == MessageBoxResult.Cancel) return;

         try
         {
            File.Delete(item.Path);
            clones.Remove(item);

            MessageBox.Show("삭제 했습니다.");
         }
         catch
         {
            MessageBox.Show("삭제에 실패 했습니다.");
         }
      }

      private void lbAutoBackup_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
      {
         ShowWorldInfo(lbAutoBackup.SelectedItem.ToString());
      }
   }
}
