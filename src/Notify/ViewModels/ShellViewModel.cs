using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;


using Notify.Helpers;
using Notify.Services;
using Notify.Views;

using Windows.System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Notify.ViewModels
{
    public class ShellViewModel : ObservableObject
    {
        private readonly KeyboardAccelerator _altLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);
        private readonly KeyboardAccelerator _backKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.GoBack);

        private IList<KeyboardAccelerator> _keyboardAccelerators;
        private WinUI.NavigationView _navigationView;
        private WinUI.NavigationViewItem _selected;
        private ICommand _loadedCommand;
        private ICommand _itemInvokedCommand;


        public WinUI.NavigationViewItem Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public ICommand LoadedCommand => _loadedCommand ?? (_loadedCommand = new RelayCommand(OnLoaded));

        public ICommand ItemInvokedCommand => _itemInvokedCommand ?? (_itemInvokedCommand = new RelayCommand<WinUI.NavigationViewItemInvokedEventArgs>(OnItemInvoked));

        public ShellViewModel()
        {
        }

        public void Initialize(Frame frame, WinUI.NavigationView navigationView, IList<KeyboardAccelerator> keyboardAccelerators)
        {
            _navigationView = navigationView;
            _keyboardAccelerators = keyboardAccelerators;
            NavigationService.Frame = frame;
            NavigationService.NavigationFailed += Frame_NavigationFailed;
            NavigationService.Navigated += Frame_Navigated;

            /*

            TODO UA307 Default back button in the title bar does not exist in WinUI3 apps.
            The tool has generated a custom back button in the MainWindow.xaml.cs file.
            Feel free to edit its position, behavior and use the custom back button instead.
            Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/case-study-1#restoring-back-button-functionality
            */_navigationView.BackRequested += OnBackRequested;
        }

        private async void OnLoaded()
        {
            // Keyboard accelerators are added here to avoid showing 'Alt + left' tooltip on the page.
            // More info on tracking issue https://github.com/Microsoft/microsoft-ui-xaml/issues/8
            _keyboardAccelerators.Add(_altLeftKeyboardAccelerator);
            _keyboardAccelerators.Add(_backKeyboardAccelerator);
            await Task.CompletedTask;
        }

        private static void OnItemInvoked(WinUI.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavigationService.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo);
            }
            else
            {
                var selectedItem = args.InvokedItemContainer as WinUI.NavigationViewItem;
                var pageType = selectedItem?.GetValue(NavHelper.NavigateToProperty) as Type;

                if (pageType != null)
                {
                    NavigationService.Navigate(pageType, null, args.RecommendedNavigationTransitionInfo);
                }
            }
        }

        private static void OnBackRequested(WinUI.NavigationView sender, WinUI.NavigationViewBackRequestedEventArgs args)
        {
            NavigationService.GoBack();
        }

        private static void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw e.Exception;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(SettingsPage))
            {
                Selected = _navigationView.SettingsItem as WinUI.NavigationViewItem;
                return;
            }

            var selectedItem = GetSelectedItem(_navigationView.MenuItems, e.SourcePageType);
            if (selectedItem != null)
            {
                Selected = selectedItem;
            }
        }

        private WinUI.NavigationViewItem GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
        {
            foreach (var item in menuItems.OfType<WinUI.NavigationViewItem>())
            {
                if (IsMenuItemForPageType(item, pageType))
                {
                    return item;
                }

                var selectedChild = GetSelectedItem(item.MenuItems, pageType);
                if (selectedChild != null)
                {
                    return selectedChild;
                }
            }

            return null;
        }

        private static bool IsMenuItemForPageType(WinUI.NavigationViewItem menuItem, Type sourcePageType)
        {
            var pageType = menuItem.GetValue(NavHelper.NavigateToProperty) as Type;
            return pageType == sourcePageType;
        }

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };
            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var result = NavigationService.GoBack();
            args.Handled = result;
        }
    }
}
