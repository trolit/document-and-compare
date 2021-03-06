﻿using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Webkit;
using docAndCom.Droid;
using docAndCom.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Collections.Generic;
using System.IO;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidCommandsActivity))]
namespace docAndCom.Droid
{
    [Activity(Label = "AndroidCommandsActivity")]
    public class AndroidCommandsActivity : Activity, IFileOpener, IFileSaver
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
        }

        public void OpenFileByGivenPath(string sourcePath)
        {
            Java.IO.File file = new Java.IO.File(sourcePath);
            Android.Net.Uri uri = FileProvider.GetUriForFile(Application.Context, "com.companyname.docandcom.fileprovider", file);
            string extension = MimeTypeMap.GetFileExtensionFromUrl(Android.Net.Uri.FromFile(file).ToString());
            string mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
            Intent intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, mimeType);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(ActivityFlags.NoHistory);
            var chooserIntent = Intent.CreateChooser(intent, "Open file");
            chooserIntent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
            Application.Context.StartActivity(chooserIntent);
        }

        public string GeneratePdfFile(List<Photo> photos, string tag, string fileName, int mode, int columnsNumber)
        {
            string root = null;

            if (ContextCompat.CheckSelfPermission(Application.Context, Manifest.Permission.WriteExternalStorage)
                != Permission.Granted)
            {
                ActivityCompat.RequestPermissions((Android.App.Activity)
                    Application.Context, new string[] { Manifest.Permission.WriteExternalStorage }, 1);
            }

            if (Android.OS.Environment.IsExternalStorageEmulated)
            {
                root = Android.OS.Environment.ExternalStorageDirectory.ToString();
            }
            else
            {
                root = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            }

            Java.IO.File myDir = new Java.IO.File(root + "/DocAndComPDFs");
            if (myDir.Exists() == false)
            {
                myDir.Mkdir();
            }

            var _path = Path.Combine(root, "DocAndComPDFs", fileName);

            FileStream fs = new FileStream(_path, FileMode.Create, FileAccess.Write);

            var doc = new iTextSharp.text.Document(PageSize.A4, 14f, 14f, 30f, 0);

            PdfWriter writer = PdfWriter.GetInstance(doc, fs);
            writer.StrictImageSequence = true;

            doc.Open();
            doc.AddTitle($"docAndCompare result for {tag} tag");
            doc.AddKeywords("docAndCompare");
            doc.AddCreator("iTextSharp lib");

            var titleFont = FontFactory.GetFont("Arial", 34.0f, Color.BLACK);
            var title = new Paragraph(ResourceLoader.Instance.GetString("pdfTitle"), titleFont);

            var subTitleFont = FontFactory.GetFont("Arial", 14.0f, Color.BLACK);
            var subTitle = new Paragraph(ResourceLoader.Instance.GetString("pdfSubTitle"), subTitleFont);

            var subSubTitleFont = FontFactory.GetFont("Arial", 10.0f, Color.BLACK);
            var subSubTitle = new Paragraph(ResourceLoader.Instance.GetString("pdfSubSubTitle") + tag, subSubTitleFont);

            doc.Add(title);
            doc.Add(subTitle);
            doc.Add(subSubTitle);

            doc.Add(new Paragraph(" ") { SpacingBefore = 8f });

            if (mode == 0) // List 
            {
                foreach (var photo in photos)
                {
                    var p = new Paragraph(
                        ResourceLoader.Instance.GetString("pdfDataText") + photo.CreatedOn.ToString("dd.MM.yyyy"));
                    p.SpacingAfter = 75f;
                    var img = Image.GetInstance(photo.Path);
                    img.ScalePercent(12f);
                    p.Alignment = Element.ALIGN_TOP;
                    img.Alignment = Element.ALIGN_MIDDLE;
                    doc.Add(p);
                    doc.Add(img);
                    doc.NewPage();
                }
            } else if (mode == 1) // Tabular
            {
                var numberOfImages = photos.Count;
                var numberOfRows = 0;

                if (numberOfImages == 3)
                {
                    numberOfRows = 2;
                }
                else if (numberOfImages > 3)
                {
                    numberOfRows = CalculateRowsAmount(numberOfImages, columnsNumber);
                }
               
                PdfPTable table = new PdfPTable(columnsNumber);
                table.KeepTogether = true;
                table.WidthPercentage = 90;

                int dateId = 0;
                int photoId = 0;

                for (int i = 0; i < numberOfRows; i++)
                {
                    for (int x = 0; x < columnsNumber; x++)  
                    {
                        // even
                        if(i % 2==0)
                        {
                            Paragraph p;

                            if(dateId < numberOfImages)
                            {
                                p = new Paragraph(photos[dateId].CreatedOn.ToString("dd.MM.yyyy"));
                            } else
                            {
                                p = new Paragraph(ResourceLoader.Instance.GetString("pdfEmptyText"));
                            }
                            p.Alignment = Element.ALIGN_CENTER;
                            table.AddCell(p);
                            dateId++;
                        } else
                        {
                            if(photoId < numberOfImages)
                            {
                                var img = Image.GetInstance(photos[photoId].Path);
                                img.ScalePercent(22f);
                                img.Alignment = Element.ALIGN_CENTER;
                                table.AddCell(img);
                            } else
                            {
                                Paragraph p = new Paragraph(ResourceLoader.Instance.GetString("pdfEmptyText"));
                                p.Alignment = Element.ALIGN_CENTER;
                                table.AddCell(p);
                            }
                            photoId++;
                        }
                    }
                }

                doc.Add(table);
            }

            doc.Close();

            return _path;
        }

        private int CalculateRowsAmount(int value, int numOfCols)
        {
            int result = 0;

            if(value == 4 && numOfCols == 3)
            {
                return 4;
            } else
            {
                for (int i = 0; i < value; i+= numOfCols)
                {
                    result += 2;
                }
            }

            return result;
        }
        //     [3 cols]     [4 cols]      [5 cols]
        // 4 => 4 rows       2 rows        2 rows
        // 5 => 4 rows       4 rows        2 rows
        // 6 => 4 rows       4 rows        4 rows
        // 7 => 6 rows       4 rows        4 rows
        // 8 => 6 rows       4 rows        4 rows
        // 9 => 6 rows       6 rows        4 rows
        // 10 => 8 rows      6 rows        4 rows
        // 11 => 8 rows      6 rows        6 rows
        // 12 => 8 rows      6 rows        6 rows
        // 13 => 10 rows     8 rows        6 rows
        // 14 => 10 rows     8 rows        6 rows
        // 15 => 10 rows     8 rows        6 rows
    }
}