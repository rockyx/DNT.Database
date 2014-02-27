using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DNT.Database
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    VehicleDB vds;
    VehicleDBTableAdapters.TableAdapterManager vdam;
    SqlConnection sqlconn;
    private string[] language = { "zh-CN", "en-US" };

    public MainWindow()
    {
      InitializeComponent();
    }

    void OnButtonOpenDatabase(object sender, RoutedEventArgs e)
    {
      try
      {
        this.IsEnabled = false;

        Task.Factory.StartNew(() =>
        {
          vds = new VehicleDB();
          vdam = new VehicleDBTableAdapters.TableAdapterManager();

          sqlconn = new SqlConnection(Properties.Settings.Default.Vehicle_Scanner_DatabaseConnectionString);
          sqlconn.Open();

          vdam.Connection = sqlconn;
          vdam.TextTableAdapter = new VehicleDBTableAdapters.TextTableAdapter();
          vdam.TextTableAdapter.Connection = sqlconn;
          vdam.TroubleCodeTableAdapter = new VehicleDBTableAdapters.TroubleCodeTableAdapter();
          vdam.TroubleCodeTableAdapter.Connection = sqlconn;
          vdam.LiveDataTableAdapter = new VehicleDBTableAdapters.LiveDataTableAdapter();
          vdam.LiveDataTableAdapter.Connection = sqlconn;
          vdam.CommandTableAdapter = new VehicleDBTableAdapters.CommandTableAdapter();
          vdam.CommandTableAdapter.Connection = sqlconn;

          vdam.TextTableAdapter.Fill(vds.Text);
          vdam.TroubleCodeTableAdapter.Fill(vds.TroubleCode);
          vdam.LiveDataTableAdapter.Fill(vds.LiveData);
          vdam.CommandTableAdapter.Fill(vds.Command);
        }).ContinueWith((t) =>
        {
          Dispatcher.BeginInvoke((Action)(() =>
          {
            this.IsEnabled = true;
            if (t.IsFaulted)
            {
              MessageBox.Show(t.Exception.InnerException.Message);
            }
            else
            {
              textDG.ItemsSource = vds.Text.DefaultView;
              troubleCodeDG.ItemsSource = vds.TroubleCode.DefaultView;
              liveDataDG.ItemsSource = vds.LiveData.DefaultView;
              commandDG.ItemsSource = vds.Command.DefaultView;
            }
          }));
        });
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
    }

    void OnButtonUpdate(object sender, RoutedEventArgs e)
    {
      try
      {
        vdam.UpdateAll(vds);
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
    }

    void OnButtonSaveSystem(object sender, RoutedEventArgs e)
    {
      SaveFileDialog dlg = new SaveFileDialog();
      dlg.Filter = "DB files (*.db)|*.db|All Files(*.*)|*.*";
      dlg.ShowDialog();

      string dbFile = dlg.FileName;
      if (string.IsNullOrEmpty(dbFile))
        throw new IOException(dbFile);

      this.IsEnabled = false;

      Task.Factory.StartNew(() =>
      {
        using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbFile))
        {
          conn.Open();

          Database db = new CryptoSystemDatabase(sqlconn, conn);
          db.CopyTo();
        }
      }).ContinueWith((t) =>
      {
        Dispatcher.BeginInvoke((Action)(() =>
        {
          this.IsEnabled = true;
        }));
      });
    }

    void OnButtonSaveVehicle(object sender, RoutedEventArgs e)
    {
      SaveFileDialog dlg = new SaveFileDialog();
      dlg.Filter = "DB files (*.db)|*.db|All Files(*.*)|*.*";
      dlg.ShowDialog();

      string dbFile = dlg.FileName;
      if (string.IsNullOrEmpty(dbFile))
        throw new IOException(dbFile);

      this.IsEnabled = false;
      string textClassText = textClass.Text;
      string troubleCodeClassText = troubleClass.Text;
      string liveDataClassText = liveDataClass.Text;
      string commandClassText = commandClass.Text;

      Task.Factory.StartNew(() =>
      {
        using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbFile))
        {
          conn.Open();

          Database text = new CryptoVehicleText(sqlconn, conn, textClassText.Split(';'));
          Database troubleCode = new CryptoVehicleTroubleCode(sqlconn, conn, troubleCodeClassText.Split(';'));
          Database liveData = new CryptoVehicleLiveData(sqlconn, conn, liveDataClassText.Split(';'));
          Database command = new CryptoVehicleCommand(sqlconn, conn, commandClassText.Split(';'));

          text.CopyTo();
          troubleCode.CopyTo();
          liveData.CopyTo();
          command.CopyTo();
        }
      }).ContinueWith((t) =>
      {
        Dispatcher.BeginInvoke((Action)(() =>
        {
          this.IsEnabled = true;
        }));
      });
    }
  }
}
