﻿using docAndCom.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using static docAndCom.Helpers.ShortenInvokes;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace docAndCom
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NewDocumentPage : ContentPage
    {
        private string pathToImage = "";

        public NewDocumentPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            FeedTagPicker();
        }

        async void TakePhoto_Clicked(object sender, System.EventArgs e)
        {
            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert(GetResourceString("OopsText"),
                    GetResourceString("cameraNotAvailableText"), 
                    GetResourceString("OkText"));
                return;
            }

            var now = DateTime.UtcNow;
            bool saveToAlbumResult = false;

            using (SQLite.SQLiteConnection conn = new SQLite.SQLiteConnection(App.DB_PATH))
            {
                var res = conn.Table<Preference>().FirstOrDefault(p => p.Key == "saveCopyToAlbum");

                if(res.Value == "true")
                {
                    saveToAlbumResult = true;
                } 
                else if (res.Value == "false")
                {
                    saveToAlbumResult = false;
                }
            }

            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                Directory = "docAndComparePhotos",
                Name = $"dac_{now:dd-MM-yyyy}_{now:HH_mm_ss}.jpg",
                SaveToAlbum = saveToAlbumResult
            });

            if (file == null)
                return;

            // await DisplayAlert("File Location", file.Path, "OK");

            pathToImage = file.Path.ToString();

            image.Source = ImageSource.FromStream(() =>
            {
                var stream = file.GetStream();
                file.Dispose();
                return stream;
            });
        }

        private async void Gallery_Clicked(object sender, EventArgs e)
        {
            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                await DisplayAlert(GetResourceString("OopsText"),
                    GetResourceString("photoPickNotSupportedText"),
                    GetResourceString("OkText"));
                return;
            }

            var file = await CrossMedia.Current.PickPhotoAsync();

            if (file == null)
                return;

            image.Source = ImageSource.FromStream(() => file.GetStream());

            pathToImage = file.Path.ToString();
        }

        private async void FeedTagPicker()
        {
            using(SQLite.SQLiteConnection conn = new SQLite.SQLiteConnection(App.DB_PATH))
            {
                conn.CreateTable<Tag>();

                var tags = conn.Table<Tag>().ToList();

                if (tags.Count == 0)
                {
                    var res = await DisplayAlert(GetResourceString("OopsText"),
                        GetResourceString("noTagText"),
                        GetResourceString("YesText"),
                        GetResourceString("NoText"));
                    
                    if(res == true)
                    {
                        var newDocPage = Navigation.NavigationStack[Navigation.NavigationStack.Count - 1];
                        await Navigation.PushAsync(new TagsPage());
                        Navigation.RemovePage(newDocPage);
                    } 
                    else
                    {
                        tagPicker.IsEnabled = false;
                    }
                }

                var tagsList = new List<string>();
                foreach (var tag in tags)
                {
                    if(tagsList.Contains(tag.Name) == false)
                    {
                        tagsList.Add(tag.Name);
                    }
                }

                tagPicker.ItemsSource = tagsList;
            }
        }

        private async void DocumentBtn_Clicked(object sender, EventArgs e)
        {
            var pickedTag = tagPicker.SelectedItem;

            if (pickedTag == null)
            {
                await DisplayAlert(GetResourceString("OopsText"),
                    GetResourceString("tagNotSelectedText"),
                    GetResourceString("OkText"));
                return;
            }

            using (SQLite.SQLiteConnection conn = new SQLite.SQLiteConnection(App.DB_PATH)) 
            {
                var id = conn.Table<Tag>().FirstOrDefault(t => t.Name == (string) pickedTag).Id;

                if (id <= 0)
                {
                    await DisplayAlert(GetResourceString("OopsText"),
                        GetResourceString("tagNotExistsText"),
                        GetResourceString("OkText"));
                    return;
                }

                conn.CreateTable<Photo>();

                var photoRes = conn.Table<Photo>().FirstOrDefault(p => p.Path == pathToImage && p.TagId == id);

                if (photoRes != null)
                {
                    await DisplayAlert(GetResourceString("OopsText"),
                        GetResourceString("imageAlreadyAddedToThatTagText"),
                        GetResourceString("OkText"));
                    return;
                }

                Photo photo = new Photo()
                {
                    Path = pathToImage,
                    CreatedOn = documentDatePicker.Date,
                    TagId = id
                };

                var numberOfRows = conn.Insert(photo);

                if (numberOfRows > 0)
                {
                    await DisplayAlert(GetResourceString("SuccessText"),
                        GetResourceString("imageSuccessfulyDocumentedText"),
                        GetResourceString("greatText"));
                }
                else
                {
                    await DisplayAlert(GetResourceString("OopsText"),
                        GetResourceString("imageNotDocumentedText"),
                        GetResourceString("OkText"));
                }
            }
        }
    }
}