// VN recommendations applet.
//
// Copyright (C) 2014 by morkt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace VNRec
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly string CacheFileName = "VNDBCache.sqlite";
        const int RecommendationLimit = 5;

        VNDB.VNDBCache          m_vndb_cache;
        VNRecSimilarUsers       m_rec_similar;
        VNRecRelativePopularity m_rec_relative;
        VNRecLinearRegression   m_rec_linear;

        private readonly BackgroundWorker m_worker = new BackgroundWorker { WorkerReportsProgress = true };

        public MainWindow ()
        {
            InitializeComponent ();
            this.UpdateButton.IsEnabled = false;
            SetBusyState();
            SetStatusText ("Initializing...");
            m_worker.DoWork += RecInit;
            m_worker.RunWorkerCompleted += RecInitComplete;
            m_worker.ProgressChanged += UpdateProgress;
        }

        void WindowLoaded (object sender, RoutedEventArgs e)
        {
            m_worker.RunWorkerAsync();
        }

        void RecInit (object sender, DoWorkEventArgs e)
        {
            string path = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filename = Path.Combine (path, CacheFileName);

            m_vndb_cache = new VNDB.VNDBCache (filename);

            m_rec_similar = new VNRecSimilarUsers (VNDB.VNDBCache.Users);
            m_rec_relative = new VNRecRelativePopularity (m_vndb_cache.Connection);
            m_rec_linear = new VNRecLinearRegression (m_vndb_cache.Connection);

//            m_rec_relative.Update (VNDB.VNDBCache.TotalGameVotes, VNDB.VNDBCache.Users);
//            m_rec_linear.Update (VNDB.VNDBCache.TotalGameVotes);
        }

        void RecInitComplete (object sender, RunWorkerCompletedEventArgs e)
        {
            Mouse.OverrideCursor = null;
            m_worker.DoWork -= RecInit;
            m_worker.RunWorkerCompleted -= RecInitComplete;
            if (null != e.Error)
            {
                MessageBox.Show (this, e.Error.Message, "Initialization error",
                                 MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
            m_worker.DoWork += UpdateRecommendations;
            m_worker.RunWorkerCompleted += UpdateComplete;
            this.UpdateButton.IsEnabled = true;
            SetStatusText ("Ready");
            StatusProgress.Visibility = Visibility.Hidden;
        }

        void UpdateProgress (object sender, ProgressChangedEventArgs e)
        {
            this.StatusProgress.Value = e.ProgressPercentage;
        }

        protected override void OnClosing (CancelEventArgs e)
        {
            if (null != m_rec_relative) m_rec_relative.Dispose();
            if (null != m_rec_linear) m_rec_linear.Dispose();
            if (null != m_vndb_cache) m_vndb_cache.Dispose();
            base.OnClosing (e);
        }

        public void SetStatusText (string text)
        {
            Dispatcher.Invoke (() => { this.StatusText.Text = text; });
        }

        void SetBusyState ()
        {
            Mouse.OverrideCursor = Cursors.Wait;
        }

        void UpdateRecommendations (object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            int total = 4;
            int progress = 0;
            worker.ReportProgress (progress++);
            var user = e.Argument as VNDB.User;
            var similar = m_rec_similar.FindRecommendations (user, RecommendationLimit);
            worker.ReportProgress (progress++ * 100 / total);
            var relative = m_rec_relative.FindRecommendations (user, RecommendationLimit);
            worker.ReportProgress (progress++ * 100 / total);
            var linear = m_rec_linear.FindRecommendations (user, RecommendationLimit);
            worker.ReportProgress (progress++ * 100 / total);
            m_vndb_cache.Update (similar.Concat (relative).Concat (linear).Select (r => r.Item1));
            worker.ReportProgress (progress++ * 100 / total);

            Dispatcher.Invoke (() =>
            {
                this.SimilarUserList.ItemsSource = similar.Select (r => new Recommendation (r));
                this.RelativePopularityList.ItemsSource = relative.Select (r => new Recommendation (r));
                this.LinearRegressionList.ItemsSource = linear.Select (r => new Recommendation (r));

                this.MostSimilarUser.DataContext = new SimilarUser (m_rec_similar.MostSimilarUser);
                this.MostSimilarUser.Visibility = Visibility.Visible;
            });
        }

        void UpdateComplete (object sender, RunWorkerCompletedEventArgs e)
        {
            Mouse.OverrideCursor = null;
            this.UpdateButton.IsEnabled = true;
            SetStatusText ("Ready");
            StatusProgress.Visibility = Visibility.Hidden;
            if (null != e.Error)
            {
                MessageBox.Show (this, e.Error.Message, "Recommendations update error",
                                 MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void Update_Click (object sender, RoutedEventArgs e)
        {
            if (m_worker.IsBusy)
                return;
            var id_string = this.UserId.Text;
            int id;
            if (!int.TryParse (id_string, out id))
            {
                this.UserId.Focus();
                return;
            }
            VNDB.User user;
            if (!VNDB.VNDBCache.Users.TryGetValue (id, out user))
            {
                MessageBox.Show (this, string.Format ("User id {0} does not exist or has very few votes.", id),
                                 "User not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.UpdateButton.IsEnabled = false;
            SetStatusText ("Calculating...");
            StatusProgress.Visibility = Visibility.Visible;
            SetBusyState();
            m_worker.RunWorkerAsync (user);
        }

        private void Hyperlink_RequestNavigate (object sender, RequestNavigateEventArgs e)
        {
            Process.Start (new ProcessStartInfo (e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }

    [ValueConversion(typeof(double), typeof(string))]
    class ScoreConverter : IValueConverter
    {
        public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
        {
            double x = (double)value;
            if (x > 10)
                return x.ToString ("F0", culture);
            else
                return x.ToString ("F2", culture);
        }

        public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
        {
            return double.Parse ((string)value, culture);
        }
    }
}
