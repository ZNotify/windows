﻿using System;

using Notify.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Notify.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
        }
    }
}