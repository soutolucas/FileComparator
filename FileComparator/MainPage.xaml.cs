using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FileComparator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private SHA256 sha256 = SHA256.Create();

        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private void btnDirectory1_Click(object sender, EventArgs e)
        {
            txtPath1.Text = GetPath();
        }

        private void btnDirectory2_Click(object sender, EventArgs e)
        {
            txtPath2.Text = GetPath();
        }

        private void btnCopyTo_Click(object sender, EventArgs e)
        {
            txtMove.Text = GetPath();
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStart.IsEnabled = false;

                await Task.Run(() =>
                {
                    var paths1 = Directory.GetFiles(txtPath1.Text, "*", SearchOption.AllDirectories);
                    var paths2 = Directory.GetFiles(txtPath2.Text, "*", SearchOption.AllDirectories);

                    CompareFiles(paths1, paths2);
                });

                await new MessageDialog("Finished").ShowAsync();
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Message:{ex.Message}; Trace: {ex.StackTrace}").ShowAsync();
            }

            btnStart.IsEnabled = true;
        }

        private void CompareFiles(string[] paths1, string[] paths2)
        {
            long fileLength1;
            long fileLength2;
            long countFiles = 0;

            Stream stream1;
            Stream stream2;

            bool disposed;

            foreach (var path1 in paths1)
            {
                disposed = false;
                stream1 = StreamRead(path1);

                countFiles++;

                txtCount.Text = $"{countFiles.ToString()}/{paths1.Length}";

                if (stream1 != null)
                {
                    fileLength1 = stream1.Length;
                    var hashFile1 = sha256.ComputeHash(stream1);

                    foreach(var path2 in paths2)
                    {
                        stream2 = StreamRead(path2);
                        if (stream2 != null)
                        {
                            fileLength2 = stream2.Length;

                            if (fileLength1 == fileLength2)
                            {
                                var hashFile2 = sha256.ComputeHash(stream2);

                                if (hashFile1.SequenceEqual(hashFile2))
                                {
                                    stream1.Dispose();
                                    disposed = true;

                                    MoveFile(path1);

                                    break;
                                }
                            }
                        }

                        stream2.Dispose();
                    }

                    if (!disposed)
                        stream1.Dispose();
                }
            }
        }

        private void MoveFile(string path1)
        {
            try
            {
                var directory = Directory.CreateDirectory(Path.Combine(txtMove.Text, Path.GetDirectoryName(path1).Replace(Path.GetPathRoot(path1), "")));
                File.Move(path1, Path.Combine(directory.FullName, Path.GetFileName(path1)));
            }
            catch (Exception ex)
            {
                new MessageDialog(ex.Message).ShowAsync().AsTask().Wait();
            }
        }

        private Stream StreamRead(string path)
        {
            try
            {
                var file = StorageFile.GetFileFromPathAsync(path).AsTask().Result;
                var stream =  file.OpenStreamForReadAsync().Result;
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
            catch
            {
                return null;
            }
        }

        private string GetPath()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = folderPicker.PickSingleFolderAsync().AsTask().Result;
            return folder.Path;
        }
    }
}
