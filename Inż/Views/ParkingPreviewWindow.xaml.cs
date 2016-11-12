﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Inż.Model;
using Inż.utils;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Window = System.Windows.Window;

namespace Inż.Views
{
    /// <summary>
    ///     Interaction logic for CounturEditorWindow.xaml
    /// </summary>
    public partial class ParkingPreviewWindow : Window
    {
        private readonly bool _initializing = true;

        private readonly DbContext _db;
        private readonly DispatcherTimer _dispatcherTimer = new DispatcherTimer(); // get progress every second
        private readonly IIageSrc _camera;

        public ParkingPreviewWindow(DbContext db, IIageSrc camera)
        {
            _db = db;
            _camera = camera;
            InitializeComponent();
            _initializing = false;

            _dispatcherTimer.Tick += dispatcherTimer_Tick;
            _dispatcherTimer.Interval = new TimeSpan(1000);
            _dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_initializing) return;
            Redraw();
        }


        private void Redraw()
        {
            var frame = _camera.GetFrame();
            //frame = frame.FastNlMeansDenoisingColored(3, 10);

            Mat saturation = frame.CvtColor(ColorConversionCodes.BGR2HSV)
                .ScaleSaturationWithValue()
                .Split()[1]
                .CvtColor(ColorConversionCodes.GRAY2BGR);
            var masks =
                _db.Contours.FindAll()
                    .Where(c => c.Pts.Any())
                    .Select(
                        contour =>
                            Gu.GetMask(contour, frame.GetSizes(),
                                Gu.SaturationTreshold(contour, frame) ? Scalar.Red : Scalar.Blue))
                    .ToList();

            masks.Insert(0, (int) ImgTypeSlider.Value == 0 ? frame : saturation);
            ImagePreview.Source = Gu.AddLayers(masks.ToArray()).ToBitmapSource();
        }


        private void EditCounturButton_OnClick(object sender, RoutedEventArgs e)
        {
            IoC.Resolve<CounturEditorWindow>().Show();
        }
    }
}