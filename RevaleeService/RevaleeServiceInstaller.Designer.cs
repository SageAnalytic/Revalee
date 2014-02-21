namespace RevaleeService
{
	partial class RevaleeServiceInstaller
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
			this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
			this.performanceCounterInstaller1 = new System.Diagnostics.PerformanceCounterInstaller();
			// 
			// serviceInstaller1
			// 
			this.serviceInstaller1.Description = "Makes web requests based on a specified time.";
			this.serviceInstaller1.DisplayName = "Revalee Service";
			this.serviceInstaller1.ServiceName = "RevaleeService";
			this.serviceInstaller1.ServicesDependedOn = new string[] {
        "HTTP"};
			this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// serviceProcessInstaller1
			// 
			this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.NetworkService;
			this.serviceProcessInstaller1.Password = null;
			this.serviceProcessInstaller1.Username = null;
			// 
			// performanceCounterInstaller1
			// 
			this.performanceCounterInstaller1.CategoryHelp = "Telemetry to monitor the activity of the Revalee Service.";
			this.performanceCounterInstaller1.CategoryName = "RevaleeService";
			this.performanceCounterInstaller1.CategoryType = System.Diagnostics.PerformanceCounterCategoryType.SingleInstance;
			this.performanceCounterInstaller1.Counters.AddRange(new System.Diagnostics.CounterCreationData[] {
            new System.Diagnostics.CounterCreationData("Awaiting Tasks", "Total number of tasks waiting to be processed.", System.Diagnostics.PerformanceCounterType.NumberOfItems32),
            new System.Diagnostics.CounterCreationData("Requests/Sec", "Number of incoming requests per second.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond32),
            new System.Diagnostics.CounterCreationData("Accepted Requests", "Total number of accepted requests.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Rejected Requests", "Total number of rejected requests.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Callbacks/Sec", "Number of callbacks processed per second.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond32),
            new System.Diagnostics.CounterCreationData("Successful Callbacks", "Total number of successfully processed callbacks.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Failed Callbacks", "Total number of callbacks that failed during processing.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Average Wait Time", "Average number of milliseconds that a callback waits for processing after its sch" +
                    "eduled time.", System.Diagnostics.PerformanceCounterType.AverageCount64),
            new System.Diagnostics.CounterCreationData("Average Wait Time base", "Base for Average Wait Time", System.Diagnostics.PerformanceCounterType.AverageBase)});
			// 
			// RevaleeServiceInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceInstaller1,
            this.serviceProcessInstaller1,
            this.performanceCounterInstaller1});

		}

		#endregion

		internal System.ServiceProcess.ServiceInstaller serviceInstaller1;
		internal System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
		internal System.Diagnostics.PerformanceCounterInstaller performanceCounterInstaller1;
	}
}