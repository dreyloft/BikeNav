﻿#pragma checksum "C:\Users\meed_rots\Documents\Visual Studio 2015\Projects\BikeNav\BikeNav\Bluetooth.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "B1FDB488D4AA666243C5FD1C09224A2E"
//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace BikeNav {
    
    
    public partial class Bluetooth : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.Grid ContentPanel;
        
        internal System.Windows.Controls.TextBlock TextBox_StatusMessages;
        
        internal System.Windows.Controls.ListBox ListBox_PairedBluetoothDevices;
        
        internal System.Windows.Controls.Button Button_ConnectToBikeNav;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/BikeNav;component/Bluetooth.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.ContentPanel = ((System.Windows.Controls.Grid)(this.FindName("ContentPanel")));
            this.TextBox_StatusMessages = ((System.Windows.Controls.TextBlock)(this.FindName("TextBox_StatusMessages")));
            this.ListBox_PairedBluetoothDevices = ((System.Windows.Controls.ListBox)(this.FindName("ListBox_PairedBluetoothDevices")));
            this.Button_ConnectToBikeNav = ((System.Windows.Controls.Button)(this.FindName("Button_ConnectToBikeNav")));
        }
    }
}
