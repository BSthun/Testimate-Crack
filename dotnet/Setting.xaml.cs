// Decompiled with JetBrains decompiler
// Type: Testiamte.Setting
// Assembly: Testiamte, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1835882B-FCD8-4F86-8423-F7808C83A056
// Assembly location: D:\Testimate\Testimate\bin\Testiamte.dll

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Testiamte.ViewModel;

namespace Testiamte
{
    public partial class Setting : Window, IComponentConnector
    {

        public Setting()
        {
            this.InitializeComponent();
            this.FetchDevices();
            this.deviceList.SelectionChanged += new SelectionChangedEventHandler(this.DeviceList_SelectionChanged);
        }

        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CameraDevice selectedItem = (sender as ComboBox).SelectedItem as CameraDevice;
            if (VideoCaptureConfig.CameraId == selectedItem.Id)
                return;
            VideoCaptureConfig.CameraId = selectedItem.Id;
            EventHandler<EventArgs> cameraDeviceChanged = MainWindow.OnCameraDeviceChanged;
            if (cameraDeviceChanged == null)
                return;
            cameraDeviceChanged((object)null, EventArgs.Empty);
        }

        private void FetchDevices()
        {
            List<CameraDevice> cameraDeviceList = new List<CameraDevice>();
            using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
            {
                int num = 0;
                using (ManagementObjectCollection.ManagementObjectEnumerator enumerator = managementObjectSearcher.Get().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        ManagementBaseObject current = enumerator.Current;
                        CameraDevice cameraDevice = new CameraDevice()
                        {
                            Id = num,
                            Name = current["Caption"].ToString()
                        };
                        cameraDeviceList.Add(cameraDevice);
                        ++num;
                        this.deviceList.Items.Add((object)cameraDevice);
                    }
                }
            }
            this.deviceList.SelectedIndex = VideoCaptureConfig.CameraId;
        }

    }
}
