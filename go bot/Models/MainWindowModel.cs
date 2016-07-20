using System.Diagnostics;
using System.Timers;

namespace GO_Bot.Models {

	internal class MainWindowModel : BindableBase {

		private PerformanceCounter cpuCounter;
		private PerformanceCounter memoryCounter;
		private float cpuUsage;
		private float memoryUsage;
		private string status;
		private int selectedTabIndex;

		private MainWindowModel() {
			Timer performanceTimer = new Timer();

			cpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
			memoryCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName);
			performanceTimer.Interval = 2000;
			performanceTimer.Elapsed += (s, e) => {
				CpuUsage = cpuCounter.NextValue();
				MemoryUsage = memoryCounter.NextValue();
				cpuCounter.NextValue();
				memoryCounter.NextValue();
			};
			performanceTimer.Start();
		}

		public static MainWindowModel Create() {
			return new MainWindowModel();
		}

		public string Status {
			get { return status; }
			set { SetProperty(ref status, value); }
		}

		public float CpuUsage {
			get { return cpuUsage; }
			set { SetProperty(ref cpuUsage, value); }
		}

		public float MemoryUsage {
			get { return memoryUsage; }
			set { SetProperty(ref memoryUsage, value); }
		}

		public int SelectedTabIndex {
			get { return selectedTabIndex; }
			set { SetProperty(ref selectedTabIndex, value); }
		}
		
	}

}
