﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace AskDB.App
{
    public static class WinUiHelper
    {
        public static async Task<ContentDialogResult> ShowErrorDialog(XamlRoot xamlRoot, string message, string title = "Error")
        {
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = title,
                Content = message,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            return await dialog.ShowAsync();
        }

        public static void SetLoading(bool isLoading, Button button, StackPanel overlayPanel, StackPanel mainPanel)
        {
            overlayPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            mainPanel.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            button.IsEnabled = !isLoading;
        }
    }
}
