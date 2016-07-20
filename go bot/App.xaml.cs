using GO_Bot.Internals;
using NLog;
using System;
using System.Windows;

namespace GO_Bot {

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			NLogConfig.Init();

#if !DEBUG
			DispatcherUnhandledException += (s, ex) => { LogManager.GetLogger("DispatcherUnhandledException").Error(ex.Exception); }; // ui thread
			AppDomain.CurrentDomain.UnhandledException += (s, ex) => { LogManager.GetLogger("AppDomainUnhandledException").Error((Exception)ex.ExceptionObject); }; // non ui threads
#endif

			Settings.Load();
		}

	}

}
