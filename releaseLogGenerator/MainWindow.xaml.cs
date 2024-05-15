using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Text.Json;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;


namespace jsonWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Check if a file has been selected
            if (string.IsNullOrEmpty(selectedFile.Content?.ToString()))
            {
                MessageBox.Show("Please select a file first.");
                return;
            }


            try
            {
                // Get the selected file path from the selectedFile label
                string selectedFilePath = selectedFile.Content.ToString();

                // Get the current directory (usually the project's directory)
                string currentDirectory = Directory.GetCurrentDirectory();

                // Traverse up to the solution directory
                DirectoryInfo directoryInfo = new DirectoryInfo(currentDirectory);
                while (directoryInfo != null && !File.Exists(Path.Combine(directoryInfo.FullName, "releaseLogGenerator.sln")))
                {
                    directoryInfo = directoryInfo.Parent;
                }

                if (directoryInfo == null)
                {
                    Console.WriteLine("Solution directory not found.");
                    return;
                }

                // Construct the path to the target file
                string solutionDirectory = directoryInfo.FullName;

                // Get the file version info
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(selectedFilePath);

                // Extract the file name without extension
                string fileName= Path.GetFileNameWithoutExtension(selectedFilePath);

                string applicationName = ""; // Set this to the desired key
                string applicationJsonPath = Path.Combine(solutionDirectory, "releaseLogGenerator", "assets", "applicationUpdate.json");

                if (File.Exists(applicationJsonPath))
                {
                    try
                    {
                        string jsonText = File.ReadAllText(applicationJsonPath);

                        // Parse the JSON text into a JsonObject
                        JsonNode jsonObj = JsonNode.Parse(jsonText);

                        // Check if the key exists in the JsonObject
                        if (jsonObj != null && jsonObj[fileName] != null)
                        {
                            // Get the value corresponding to the key
                            JsonNode value = jsonObj[fileName]["updatePath"];
                            applicationName = value.ToString();
                        }
                        else
                        {
                            MessageBox.Show("Given file name not found in applicationUpdate.json");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("The JSON file does not exist.");
                    return;
                }

                string formattedVersion = $"{versionInfo.ProductMajorPart}.{versionInfo.ProductMinorPart}.{versionInfo.ProductBuildPart}/{versionInfo.ProductMajorPart}.{versionInfo.ProductMinorPart}.{versionInfo.ProductBuildPart}+{versionInfo.ProductPrivatePart}";

                // Construct the URL based on the application name and file version
                string downloadUrl = $"https://www.peregrineconnect.com/support/downloads/{applicationName}/{formattedVersion}/{Path.GetFileName(selectedFilePath)}";

                ReleaseNotes releaseNotes = new ReleaseNotes
                {
                    NewFeatures = new string[] { },
                    BugFixes = new string[] { },
                    Improvements = new string[] { },
                    Miscellaneous = new string[] { }
                };

                // For textarea
                string FeaturesText = txtAreaFeatures.Text;
                string[] FeaturesLines = FeaturesText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                releaseNotes.NewFeatures = FeaturesLines;

                string BugFixesText = txtAreaBugFixes.Text;
                string[] BugFixesLines = BugFixesText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                releaseNotes.BugFixes = BugFixesLines;

                string ImprovementsText = txtAreaImprovements.Text;
                string[] ImprovementsLines = ImprovementsText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                releaseNotes.Improvements = ImprovementsLines;

                string MiscellaneousText = txtAreaMiscellaneous.Text;
                string[] MiscellaneousLines = MiscellaneousText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                releaseNotes.Miscellaneous = MiscellaneousLines;

                var fileInfo = new
                {
                    version = $"{ versionInfo.ProductMajorPart}.{ versionInfo.ProductMinorPart}.{ versionInfo.ProductBuildPart}+{ versionInfo.ProductPrivatePart}",
                    size = new FileInfo(selectedFilePath).Length,
                    date = File.GetLastWriteTime(selectedFilePath),
                    url = downloadUrl,
                    ReleaseNotes = releaseNotes
                };

                // Create JsonSerializerOptions with UnsafeRelaxedJsonEscaping
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };

                // Serialize the object to JSON using JsonSerializerOptions
                string jsonString = JsonSerializer.Serialize(fileInfo, options);

                string targetFilePath = Path.Combine(solutionDirectory, "releaseLogGenerator", "assets", "details.json");

                File.WriteAllText(targetFilePath, jsonString);

                MessageBox.Show("Details saved to JSON file.");

                Clipboard.SetText(jsonString);

                MessageBox.Show("Details copied to clipboard successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: \n" + ex.Message);
            }
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "Executable Files (*.exe;*.msi)|*.exe;*.msi|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string selectedFileName = openFileDialog.FileName;

                selectedFile.Content = selectedFileName;
            }
        }
    }

    public class ReleaseNotes
    {
        public string[] NewFeatures { get; set; }
        public string[] BugFixes { get; set; }
        public string[] Improvements { get; set; }
        public string[] Miscellaneous { get; set; }
    }
}