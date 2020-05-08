using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace scte_104_inserter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Mutex _mtx = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            String mtxName = "scte_104_inserter";
            bool isCreateNew = false;
            try
            {
                _mtx = new Mutex(true, mtxName, out isCreateNew);

                if ( isCreateNew )
                {
                    base.OnStartup(e);
                } else
                {
                    MessageBox.Show("프로세스 중복 실행이 감지 되었습니다.\n프로그램을 종료 합니다.", "경고", MessageBoxButton.OK);
                    Application.Current.Shutdown();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "경고", MessageBoxButton.OK);
                Application.Current.Shutdown();
            }
            
        }
    }
}
