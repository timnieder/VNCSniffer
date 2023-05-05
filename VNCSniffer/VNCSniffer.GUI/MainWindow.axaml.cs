using Avalonia.Controls;
using Avalonia.Threading;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VNCSniffer.Core;
using VNCSniffer.GUI.ViewModels;

namespace VNCSniffer.GUI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public Dictionary<Connection, ConnectionTabViewModel> Connection = new();
        public TabItem InterfaceTab;
        private ObservableCollection<TabItem> items = new();
        private int selectedTab;

        #region Bindings
        // Needed to notify the view that a property has changed
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<TabItem> Items
        {
            get => items;

            set
            {
                if (value != items)
                {
                    items = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int SelectedTab
        {
            get => selectedTab;
            set
            {
                if (value != selectedTab) 
                {
                    selectedTab = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            items.Add(new TabItem() { IsVisible = false, Content = "No connections yet..." }); //TODO: make it into own "loading" tab? also add info about the current interface
            InterfaceTab = new TabItem()
            {
                Header = "Interfaces",
                Content = new InterfaceTab(new InterfaceTabViewModel(this)),
            };
            items.Add(InterfaceTab);
            SelectedTab = 1; // select interface tab
            //TODO: add reset button
        }

        public void CreateNewTabForConnection(Connection con)
        {
            if (Connection.ContainsKey(con))
                throw new Exception("Connection already has a tab.");

            var tab = new ConnectionTabViewModel(con);
            // put into connection->tab lookup for later
            Connection.Add(con, tab);
            Dispatcher.UIThread.Post(() =>
            {
                // hook up tab
                var control = new TabItem()
                {
                    Header = "Connection",
                    Content = new ConnectionTab(tab),
                };
                items.Add(control);
            }, DispatcherPriority.MaxValue);
        }

        public void ResizeFramebuffer(Connection con, int width, int height)
        {
            if (!Connection.TryGetValue(con, out var tab))
                throw new Exception($"Tab for Connection {con} doesn't exist.");

            tab.ResizeFramebuffer(con, width, height);
        }

        public void RefreshFramebuffer(Connection con)
        {
            if (!Connection.TryGetValue(con, out var tab))
                throw new Exception($"Tab for Connection {con} doesn't exist.");

            tab.RefreshFramebuffer(con);
        }

        public void OnDeviceSelected(ICaptureDevice device)
        {
            this.InterfaceTab.IsVisible = false;
            SelectedTab = 0;

            if (Design.IsDesignMode)
                return;
            Sniffer.Start(this, device);
        }
    }
}
