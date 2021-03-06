﻿using docAndCom.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static docAndCom.Helpers.ShortenInvokes;

namespace docAndCom
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GeneratePdfPage : ContentPage
    {
        public GeneratePdfPage()
        {
            InitializeComponent();

            FeedTagPicker();
        }

        protected override void OnAppearing()
        {
            List<string> docTypeList = new List<string>
            {
                GetResourceString("generateFilepickerList"),
                GetResourceString("generateFilepickerTabular")
            };
            docTypePicker.ItemsSource = docTypeList;
        }

        private async void GeneratePdf_Clicked(object sender, EventArgs e)
        {
            ai.IsRunning = true;
            aiLayout.IsVisible = true;
            await Task.Delay(2000);

            var tag = tagPicker.SelectedItem;
            var docScheme = docTypePicker.SelectedIndex;

            if (tag == null)
            {
                await DisplayAlert(GetResourceString("OopsText"),
                    GetResourceString("tagNotChosenText"),
                    GetResourceString("OkText"));
                return;
            } else if (docScheme != 0 && docScheme != 1)
            {
                await DisplayAlert(GetResourceString("OopsText"),
                    GetResourceString("fileSchemeNotChosenText"),
                    GetResourceString("OkText"));
                return;
            }

            var tagName = tag.ToString();
            int numberOfImages = 0;
            int numberOfCols = 0;
            var createdOn = DateTime.UtcNow;
            List<Photo> photos = new List<Photo>();

            using (SQLite.SQLiteConnection conn = new SQLite.SQLiteConnection(App.DB_PATH))
            {
                conn.CreateTable<Models.Document>();

                int tagId = conn.Table<Tag>().FirstOrDefault(t => t.Name == tagName).Id;

                numberOfImages = conn.Table<Photo>().Where(p => p.TagId == tagId).Count();

                var tmp = conn.Table<Preference>().FirstOrDefault(p => p.Key == "numOfCol");

                if(tmp != null)
                {
                    numberOfCols = Convert.ToInt32(tmp.Value);
                } 
                else
                {
                    numberOfCols = 3;
                }

                photos = conn.Table<Photo>().Where(p => p.TagId == tagId).ToList();
            }

            var fileName = $"{tagName}_{createdOn:dd-MM-yyyy}_{createdOn:HH-mm-ss}.pdf";

            var filePath = DependencyService.Get<IFileSaver>().GeneratePdfFile
                (photos, tagName, fileName, docScheme, numberOfCols);

            using (SQLite.SQLiteConnection conn = new SQLite.SQLiteConnection(App.DB_PATH))
            {
                Models.Document doc = new Models.Document()
                {
                    FileName = fileName,
                    Path = filePath,
                    GeneratedOn = createdOn
                };

                var res = conn.Insert(doc);

                ai.IsRunning = false;
                aiLayout.IsVisible = false;

                if (res > 0)
                {
                    await DisplayAlert(GetResourceString("SuccessText"),
                        GetResourceString("fileGeneratedText"), 
                        GetResourceString("greatText"));

                    var genPdfPage = Navigation.NavigationStack[Navigation.NavigationStack.Count - 1];
                    var genPage = Navigation.NavigationStack[Navigation.NavigationStack.Count - 2];
                    await Navigation.PushAsync(new GeneratePage());
                    Navigation.RemovePage(genPage);
                    Navigation.RemovePage(genPdfPage);
                } 
                else
                {
                    if(File.Exists(filePath))
                    {
                        var alertMsg = GetResourceString("fileRefNotSavedButFileGenerated");
                        alertMsg = alertMsg.Replace("<%filePath%>", filePath);

                        await DisplayAlert(GetResourceString("OopsText"),
                            alertMsg, 
                            GetResourceString("OkText"));
                    } else
                    {
                        await DisplayAlert(GetResourceString("OopsText"),
                            GetResourceString("fileRefNotSavedNotGenerated"), 
                            GetResourceString("OkText"));
                    }
                }
            }
        }

        private async void FeedTagPicker()
        {
            using (SQLite.SQLiteConnection conn = new SQLite.SQLiteConnection(App.DB_PATH))
            {
                conn.CreateTable<Tag>();

                var tags = conn.Table<Tag>().ToList();

                var tagsList = new List<string>();
                foreach (var tag in tags)
                {
                    var numberOfImages = conn.Table<Photo>().Where(p => p.TagId == tag.Id).Count();

                    if (tagsList.Contains(tag.Name) == false && numberOfImages > 2)
                    {
                        tagsList.Add(tag.Name);
                    }
                }

                if(tagsList.Count <= 0)
                {
                    await DisplayAlert(GetResourceString("OopsText"),
                        GetResourceString("notEnoughImages"),
                        GetResourceString("OkText"));

                    createDocBtn.IsEnabled = false;
                }

                tagPicker.ItemsSource = tagsList;
            }
        }
    }
}