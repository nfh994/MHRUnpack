using MHRUnpack.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace MHRUnpack
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static new App Current => (App)Application.Current;

        [STAThread]
        public static void Main()
        {
            //#region 进程锁
            //using var mutex = new Mutex(true, "MHRUnpack");
            //if (!mutex.WaitOne(TimeSpan.Zero, true))
            //{
            //    MessageBox.Show("已有程序运行中!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}
            //#endregion

            App app = new App();
            app.InitializeComponent();
            var window = ServiceManager.Services.GetService<MainWindow>();
            app.MainWindow = window;
            window.Show();
            app.Run();
        }
    }

}
