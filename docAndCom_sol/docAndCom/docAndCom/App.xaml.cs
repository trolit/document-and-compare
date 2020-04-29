﻿using docAndCom.Models;
using docAndCom.Resources;
using System.Globalization;
using Xamarin.Forms;

namespace docAndCom
{
    public partial class App : Application
    {
        public static string DB_PATH = string.Empty;

        public static ResourceLoader ResLoader;

        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
        }

        public App(string DB_Path)
        {
            InitializeComponent();

            DB_PATH = DB_Path;

            ResLoader = new ResourceLoader(AppResources.ResourceManager);

            LoadLanguage();

            SetDefaultNumberOfColumnsForTabularScheme();

            MainPage = new NavigationPage(new MainPage());
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }

        private void SetDefaultNumberOfColumnsForTabularScheme()
        {
            using (SQLite.SQLiteConnection conn = new SQLite.SQLiteConnection(DB_PATH))
            {
                conn.CreateTable<Preference>();

                var res = conn.Table<Preference>().FirstOrDefault(p => p.Key == "numOfCol");

                if(res == null)
                {
                    Preference pref = new Preference()
                    {
                        Key = "numOfCol",
                        Value = "3"
                    };

                    conn.Insert(pref);
                }
            }
        }

        private void LoadLanguage()
        {
            using (SQLite.SQLiteConnection conn = new SQLite.SQLiteConnection(DB_PATH))
            {
                conn.CreateTable<Preference>();

                var res = conn.Table<Preference>().FirstOrDefault(p => p.Key == "lang");

                if (res != null)
                {
                    var newCulture = new CultureInfo(res.Value, false);

                    ResLoader.SetCultureInfo(newCulture);
                }
                else
                {
                    var currentUiCulture = CultureInfo.CurrentUICulture;

                    Preference pref = new Preference()
                    {
                        Key = "lang"
                    };

                    if (currentUiCulture.Name == "pl-PL")
                    {
                        pref.Value = "pl-PL";
                    } else
                    {
                        pref.Value = "en-US";
                    }

                    conn.Insert(pref);

                    var defaultCulture = new CultureInfo(pref.Value, false);

                    ResLoader.SetCultureInfo(defaultCulture);
                }
            }
        }
    }
}
