// Copyright (C) 2009-2023 Lemoine Automation Technologies
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

using Lemoine.I18N;
using Lemoine.Info;
using Lemoine.Model;
using Lemoine.ModelDAO;
using Lemoine.Core.Log;
using Microsoft.Win32;
using Lemoine.Info.ConfigReader.TargetSpecific;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.Versioning;
using Lemoine.Collections;
using Lemoine.Database.Persistent;
using Lemoine.ServiceTools;

namespace LemoineServiceMonitoring
{
  /// <summary>
  /// Description of MainForm.
  /// </summary>
  public partial class MainForm : Form
  {
    static readonly string ADDITIONAL_LCTR_KEY = "AdditionalServices.Lctr";
    static readonly string ADDITIONAL_LPOST_KEY = "AdditionalServices.Lpost";
    static readonly string ADDITIONAL_OTHERS_KEY = "AdditionalServices.Others";
    static readonly string ADDITIONAL_DEFAULT = ""; // List string, first character is the separator

    static readonly string COMPUTERS_LCTR_KEY = "computers.lctr";
    static readonly bool COMPUTERS_LCTR_DEFAULT = true;

    static readonly string COMPUTERS_LPOSTS_KEY = "computers.lposts";
    static readonly bool COMPUTERS_LPOSTS_DEFAULT = true;

    static readonly string COMPUTERS_ALERT_KEY = "computers.alert";
    static readonly bool COMPUTERS_ALERT_DEFAULT = true;

    static readonly string COMPUTERS_AUTOREASON_KEY = "computers.autoReason";
    static readonly bool COMPUTERS_AUTOREASON_DEFAULT = true;

    static readonly string COMPUTERS_SYNCHRONIZATION_KEY = "computers.synchronization";
    static readonly bool COMPUTERS_SYNCHRONIZIATION_DEFAULT = true;

    static readonly string COMPUTERS_WEB_KEY = "computers.web";
    static readonly bool COMPUTERS_WEB_DEFAULT = true;

    static readonly string COMPUTERS_CONTROLS_KEY = "computers.controls";
    static readonly bool COMPUTERS_CONTROLS_DEFAULT = true;

    /// <summary>
    /// Start or stop sequentially the selected services
    /// </summary>
    static readonly string SEQUENTIAL_KEY = "ServiceMonitoring.Sequential";
    static readonly bool SEQUENTIAL_DEFAULT = false;

    private static readonly ILog log = LogManager.GetLogger (typeof (MainForm).FullName);

    volatile int m_listViewUpdate = 0;

    #region Constructors
    /// <summary>
    /// Main form of the Lemoine Service Monitoring application
    /// </summary>
    public MainForm ()
    {
      //
      // The InitializeComponent() call is required for Windows Forms designer support.
      //
      InitializeComponent ();
    }

    private async void MainForm_Load (object sender, EventArgs e)
    {
      log.Debug ("Lem_ServiceMonitoring /B");

      //
      // Make the translations
      //
      try {
        this.buttonStart.Text = PulseCatalog.GetString ("Start");
        this.buttonStop.Text = PulseCatalog.GetString ("Stop");
        this.buttonRestart.Text = PulseCatalog.GetString ("Restart");
        this.startToolStripMenuItem.Text = PulseCatalog.GetString ("Start");
        this.stopToolStripMenuItem.Text = PulseCatalog.GetString ("Stop");
        this.restartToolStripMenuItem.Text = PulseCatalog.GetString ("Restart");
        this.ButtonStartAll.Text = PulseCatalog.GetString ("StartAll");
        this.ButtonStopAll.Text = PulseCatalog.GetString ("StopAll");
        this.buttonRefresh.Text = PulseCatalog.GetString ("Refresh");
        this.autoToolStripMenuItem.Text = PulseCatalog.GetString ("Auto");
        this.manualToolStripMenuItem.Text = PulseCatalog.GetString ("Manual");
        this.disabledToolStripMenuItem.Text = PulseCatalog.GetString ("Disabled");
        this.dependenciesToolStripMenuItem.Text = PulseCatalog.GetString ("Dependencies");
        this.dependsOnToolStripMenuItem.Text = PulseCatalog.GetString ("DependsOn");
        this.columnDisplay.Text = PulseCatalog.GetString ("ServiceDisplayName");
        this.columnName.Text = PulseCatalog.GetString ("ServiceName");
        this.columnStatus.Text = PulseCatalog.GetString ("Status");
        this.columnStartupType.Text = PulseCatalog.GetString ("Startup Type");
      }
      catch (Exception) {
        // The resources DLL file was not found:
        // do not translate the texts
      }

      //
      // Add constructor code after the InitializeComponent() call.
      //

      // 1. Service names

      // Common service to L_Post, L_Ctr, ...
      var commonServices = new List<string> {
        "Lem_WatchDogService" // Lemoine WatchDog Service
      };

      // 1.a) LCTR
      var lctrServices = new List<string> {
        "Lem_AnalysisService", // Lemoine Analysis Service
        "Lem_AlertService", // [Optional] Lemoine Alert Service
        "Lem_AutoReasonService", // [Optional] Lemoine Auto-reason Service
        "Lem_SynchronizationService", // [Optional] Lemoine Synchronization
        "Lem_AspService", // Lemoine Asp Service
        "Lem_PFR", // Pulse File Repository
        "MSG_Svc", // Messenging
        "pulse_shm_svc", // System Health Monitor
        "Lem_StampingService" // Lemoine Stamping Service
      };
      AddAdditionalServices (lctrServices, ADDITIONAL_LCTR_KEY);

      // 1.b) LPOSTS
      var lpostServices = new List<string> {
        "Lem_CncService", // New Cnc Service
        "Lem_CncCoreService", // New Cnc Service
        "Lem_CncDataService", // New Cnc Data Service
        "MTConnect Agent", // MTConnect agent on LPost
        "MTConnect Agent 1", // MTConnect agent on LPost
        "MTConnect Agent 2", // MTConnect agent on LPost
        "MTConnect Agent 3", // MTConnect agent on LPost
        "MTConnect Agent 4", // MTConnect agent on LPost
        "MTConnect Agent 5", // MTConnect agent on LPost
        "MTConnect Agent 6", // MTConnect agent on LPost
        "MTConnect Agent 7", // MTConnect agent on LPost
        "MTConnect Agent 8", // MTConnect agent on LPost
        "MTConnect Agent 9" // MTConnect agent on LPost
      };
      AddAdditionalServices (lpostServices, ADDITIONAL_LPOST_KEY);

      // Alert
      var alertServices = new List<string> {
        "Lem_AlertService" // Lemoine Alert Service
      };

      // AutoReason
      var autoReasonServices = new List<string> {
        "Lem_AutoReasonService" // Lemoine Auto-reason Service
      };

      // Synchronization
      var synchronizationServices = new List<string> {
        "Lem_SynchronizationService" // Lemoine Synchronization
      };

      // 1.c) web
      var webServices = new List<string> {
        "Lem_AspService" // Lemoine Asp Service
      };

      // 1.d) Controls
      var controlServices = new List<string> {
        "Lem_Control", // Control des services
        "Lem_SiemensToCorbaService", // Lemoine SiemensToCorba Service
        "Lem_SiemensToCorba", // Old Lemoine SiemensToCorba Service name
        "Lem_FanucToCorbaService"
      };

      // 1.e) Extras
      var otherServices = new List<string> {
        "TAO_NT_Naming_Service", // TAO
        "omninames" // Omni name service
      };
      RegistryKey rk = Registry.LocalMachine.OpenSubKey ("Software\\PostgreSQL\\Services");
      if (null != rk) {
        foreach (string psqlService in rk.GetSubKeyNames ()) {
          otherServices.Add (psqlService);
        }
      }
      otherServices.Add ("postgresql-x64-9.6"); // because the 64bits programs are not in the same registry key
      otherServices.Add ("Tomcat7");
      otherServices.Add ("Tomcat8");
      otherServices.Add ("Tomcat9");
      otherServices.Add ("nscp"); // NSClient++ for Nagios
      otherServices.Add ("Lemoine Loop"); // For tests
      otherServices.Add ("MTConnectAdapterService");
      AddAdditionalServices (otherServices, ADDITIONAL_OTHERS_KEY);

      // 2. Add the services
      int groupIndex = -1;
      IDAOFactory daoFactory = ModelDAOHelper.DAOFactory;

      IList<Task> addServiceTasks = new List<Task> ();

      // Lemoine services on this computer
      groupIndex++;
      var lemoineGroup = new ListViewGroup ("This computer: Lemoine services");
      lemoineGroup.Name = "LemoineGroup";
      listView.Groups.Insert (groupIndex, lemoineGroup);
      foreach (var serviceName in lctrServices) {
        addServiceTasks.Add (AddServiceAsync (serviceName, groupIndex));
      }
      foreach (var serviceName in lpostServices) {
        addServiceTasks.Add (AddServiceAsync (serviceName, groupIndex));
      }
      foreach (var serviceName in controlServices) {
        addServiceTasks.Add (AddServiceAsync (serviceName, groupIndex));
      }
      foreach (var serviceName in commonServices) {
        addServiceTasks.Add (AddServiceAsync (serviceName, groupIndex));
      }

      // 2.d) Extras services on this computer
      groupIndex++;
      var othersGroup = new ListViewGroup ("This computer: other services");
      othersGroup.Name = "OthersGroup";
      listView.Groups.Insert (groupIndex, othersGroup);
      foreach (String ServiceName in otherServices) {
        addServiceTasks.Add (AddServiceAsync (ServiceName, groupIndex));
      }

      // Run first the services on the local computer
      await Task.WhenAll (addServiceTasks);
      addServiceTasks.Clear ();

      IComputer lctr = null;
      // 2.a) LCtr
      if (Lemoine.Info.ConfigSet.LoadAndGet<bool> (COMPUTERS_LCTR_KEY, COMPUTERS_LCTR_DEFAULT)) {
        using (IDAOSession daoSession = daoFactory.OpenSession ()) {
          lctr = await daoFactory.ComputerDAO.GetLctrAsync ();
        }

        if ((null != lctr) && !lctr.IsLocal ()) {
          groupIndex++;
          ListViewGroup lctrLemoineGroup =
            new ListViewGroup ($"LCtr {lctr.Name} ({lctr.Address}): Lemoine services");
          listView.Groups.Insert (groupIndex, lctrLemoineGroup);
          foreach (var serviceName in lctrServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, lctr.Address, groupIndex));
          }
          foreach (var serviceName in commonServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, lctr.Address, groupIndex));
          }
          groupIndex++;
          ListViewGroup lctrOthersGroup =
            new ListViewGroup ($"LCtr {lctr.Name} ({lctr.Address}): other services");
          listView.Groups.Insert (groupIndex, lctrOthersGroup);
          foreach (String ServiceName in otherServices) {
            addServiceTasks.Add (AddServiceAsync (ServiceName, lctr.Address, groupIndex));
          }
        }
      }

      // 2.b) LPosts
      if (Lemoine.Info.ConfigSet.LoadAndGet<bool> (COMPUTERS_LPOSTS_KEY, COMPUTERS_LPOSTS_DEFAULT)) {
        IList<IComputer> lposts;

        using (IDAOSession daoSession = daoFactory.OpenSession ()) {
          lposts = await daoFactory.ComputerDAO.GetLpostsAsync ();
        }

        foreach (IComputer lpost in lposts.Where (x => !x.IsLocal ())) {
          groupIndex++;
          ListViewGroup lpostGroup =
            new ListViewGroup ($"LPost: {lpost.Name} ({lpost.Address})");
          listView.Groups.Insert (groupIndex, lpostGroup);
          foreach (var serviceName in lpostServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, lpost.Address, groupIndex));
          }
          foreach (var serviceName in commonServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, lpost.Address, groupIndex));
          }
        }
      }

      // Alert
      if (Lemoine.Info.ConfigSet.LoadAndGet<bool> (COMPUTERS_ALERT_KEY, COMPUTERS_ALERT_DEFAULT)) {
        IComputer alertMachine;

        using (IDAOSession daoSession = daoFactory.OpenSession ()) {
          alertMachine = await daoFactory.ComputerDAO.GetAlertAsync ();
        }
        if ((null != alertMachine) && !object.Equals (alertMachine?.Id, lctr?.Id) && !alertMachine.IsLocal ()) {
          groupIndex++;
          ListViewGroup controlGroup =
            new ListViewGroup ($"Alert: {alertMachine.Name} ({alertMachine.Address})");
          listView.Groups.Insert (groupIndex, controlGroup);
          foreach (var serviceName in alertServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, alertMachine.Address, groupIndex));
          }
          foreach (var serviceName in commonServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, alertMachine.Address, groupIndex));
          }
        }
      }

      // Auto-reason
      if (Lemoine.Info.ConfigSet.LoadAndGet<bool> (COMPUTERS_AUTOREASON_KEY, COMPUTERS_AUTOREASON_DEFAULT)) {
        IComputer autoReasonMachine;

        using (IDAOSession daoSession = daoFactory.OpenSession ()) {
          autoReasonMachine = await daoFactory.ComputerDAO.GetAutoReasonAsync ();
        }
        if ((null != autoReasonMachine) && !object.Equals (autoReasonMachine?.Id, lctr?.Id) && !autoReasonMachine.IsLocal ()) {
          groupIndex++;
          ListViewGroup controlGroup =
            new ListViewGroup ($"Auto-reason: {autoReasonMachine.Name} ({autoReasonMachine.Address})");
          listView.Groups.Insert (groupIndex, controlGroup);
          foreach (var serviceName in autoReasonServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, autoReasonMachine.Address, groupIndex));
          }
          foreach (var serviceName in commonServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, autoReasonMachine.Address, groupIndex));
          }
        }
      }

      // Synchronization
      if (Lemoine.Info.ConfigSet.LoadAndGet<bool> (COMPUTERS_SYNCHRONIZATION_KEY, COMPUTERS_SYNCHRONIZIATION_DEFAULT)) {
        IComputer synchronizationMachine;

        using (IDAOSession daoSession = daoFactory.OpenSession ()) {
          synchronizationMachine = await daoFactory.ComputerDAO.GetSynchronizationAsync ();
        }
        if ((null != synchronizationMachine) && !object.Equals (synchronizationMachine?.Id, lctr?.Id) && !synchronizationMachine.IsLocal ()) {
          groupIndex++;
          ListViewGroup controlGroup =
            new ListViewGroup ($"Synchronization: {synchronizationMachine.Name} ({synchronizationMachine.Address})");
          listView.Groups.Insert (groupIndex, controlGroup);
          foreach (var serviceName in synchronizationServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, synchronizationMachine.Address, groupIndex));
          }
          foreach (var serviceName in commonServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, synchronizationMachine.Address, groupIndex));
          }
        }
      }

      // 2.c) Web
      if (Lemoine.Info.ConfigSet.LoadAndGet<bool> (COMPUTERS_WEB_KEY, COMPUTERS_WEB_DEFAULT)) {
        IList<IComputer> webMachines;

        using (IDAOSession daoSession = daoFactory.OpenSession ()) {
          webMachines = await daoFactory.ComputerDAO.GetWebAsync ();
        }

        foreach (IComputer webMachine in webMachines.Where (x => !object.Equals (x?.Id, lctr?.Id) && !x.IsLocal ())) {
          groupIndex++;
          ListViewGroup controlGroup =
            new ListViewGroup ($"Web: {webMachine.Name} ({webMachine.Address})");
          listView.Groups.Insert (groupIndex, controlGroup);
          foreach (var serviceName in webServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, webMachine.Address, groupIndex));
          }
          foreach (var serviceName in commonServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, webMachine.Address, groupIndex));
          }
        }
      }

      // 2.c) Controls
      if (Lemoine.Info.ConfigSet.LoadAndGet<bool> (COMPUTERS_CONTROLS_KEY, COMPUTERS_CONTROLS_DEFAULT)) {
        IList<IComputer> cncMachines;

        using (IDAOSession daoSession = daoFactory.OpenSession ()) {
          cncMachines = await daoFactory.ComputerDAO.GetCncAsync ();
        }

        foreach (IComputer cncMachine in cncMachines.Where (x => !string.IsNullOrEmpty (x.Address) && !x.IsLocal ())) {
          groupIndex++;
          ListViewGroup controlGroup =
            new ListViewGroup ($"Control: {cncMachine.Name} ({cncMachine.Address})");
          listView.Groups.Insert (groupIndex, controlGroup);
          foreach (var serviceName in controlServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, cncMachine.Address, groupIndex));
          }
          foreach (var serviceName in lpostServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, cncMachine.Address, groupIndex));
          }
          foreach (var serviceName in commonServices) {
            addServiceTasks.Add (AddServiceAsync (serviceName, cncMachine.Address, groupIndex));
          }
        }
      }

      await Task.WhenAll (addServiceTasks);
    }

    void AddAdditionalServices (IList<string> services, string configurationKey)
    {
      var additionalServices = Lemoine.Info.ConfigSet.LoadAndGet (configurationKey, ADDITIONAL_DEFAULT);
      if (!string.IsNullOrEmpty (additionalServices)) {
        foreach (var additionalService in EnumerableString.ParseListString (additionalServices)) {
          services.Add (additionalService);
        }
      }
    }

    #endregion

    #region Methods
    async Task StopSelectedServicesAsync (ListView listView)
    {
      var listViewItems = listView.SelectedItems;
      var tasks = new List<Task> ();
      for (int i = 0; i < listViewItems.Count; ++i) {
        var listViewItem = listViewItems[i];
        tasks.Add (new Task (async () => await StopServiceWithProgressAsync (listViewItem)));
      }
      await RunTasks (tasks);
    }

    async Task StopServicesAsync (IEnumerable<ListViewItem> listViewItems)
    {
      var tasks = new List<Task> ();
      foreach (var listViewItem in listViewItems) {
        tasks.Add (new Task (async () => await StopServiceWithProgressAsync (listViewItem)));
      }
      await RunTasks (tasks);
    }

    async Task StopServiceWithProgressAsync (ListViewItem listViewItem)
    {
      WindowsServiceController sc = new WindowsServiceController (listViewItem.Text,
                                                       listViewItem.SubItems[1].Text);
      try {
        await StopServiceAsync (listViewItem, sc);
      }
      catch (Exception ex) {
        log.Error ($"StopServiceWithProgressAsync: {listViewItem.Text} could not be stopped", ex);
        // raise a message
        MessageBox.Show ($"The service {listViewItem.Text} could not be stopped. Did you run the program with the Administrator privilege ?",
                        "Lemoine Service Monitoring",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
      }
      this.Invoke (new MethodInvoker (progressBar.PerformStep));
      await UpdateStatusAsync (listViewItem, sc);
    }

    async Task StopServicesAsync (IEnumerable<WindowsServiceController> serviceControllers)
    {
      var tasks = serviceControllers
        .Select (sc => sc.StopServiceAsync ());
      await RunTasks (tasks);
    }

    async Task StopServiceAsync (ListViewItem listViewItem, WindowsServiceController sc)
    {
      switch (sc.Status) {
      case ServiceControllerStatus.Running:
        InvokeSetStatus (listViewItem, GetTranslation ("Stopping"), sc.StartMode);
        break;
      default:
        break;
      }
      await sc.StopServiceAsync ();
      await UpdateStatusAsync (listViewItem, sc);
    }

    async Task RunTasks (IEnumerable<Task> tasks)
    {
      var sequential = Lemoine.Info.ConfigSet.LoadAndGet (SEQUENTIAL_KEY, SEQUENTIAL_DEFAULT);
      if (sequential) {
        foreach (var task in tasks) {
          task.Start ();
          await Task.WhenAll (task);
        }
      }
      else {
        foreach (var task in tasks) {
          task.Start ();
        }
        await Task.WhenAll (tasks);
      }
    }

    async Task StartSelectedServicesAsync (ListView listView)
    {
      var listViewItems = listView.SelectedItems;
      var tasks = new List<Task> ();
      for (int i = 0; i < listViewItems.Count; ++i) {
        var listViewItem = listViewItems[i];
        tasks.Add (new Task (async () => await StartServiceWithProgressAsync (listViewItem)));
      }
      await RunTasks (tasks);
    }

    async Task StartServicesAsync (IEnumerable<ListViewItem> listViewItems, bool autoOnly = false)
    {
      var tasks = new List<Task> ();
      foreach (var listViewItem in listViewItems) {
        tasks.Add (new Task (async () => await StartServiceWithProgressAsync (listViewItem, autoOnly)));
      }
      await RunTasks (tasks);
    }

    async Task StartServiceWithProgressAsync (ListViewItem listViewItem, bool autoOnly = false)
    {
      WindowsServiceController sc = new WindowsServiceController (listViewItem.Text,
                                                       listViewItem.SubItems[1].Text);

      if (autoOnly && !sc.StartupType.Equals (StartupType.Auto)) {
        this.Invoke (new MethodInvoker (progressBar.PerformStep));
        return;
      }

      try {
        await StartServiceAsync (listViewItem, sc);
      }
      catch (Exception ex) {
        log.Error ($"StartServiceWithProgressAsync: {listViewItem.Text} could not be started", ex);
        // raise a message
        MessageBox.Show ($"The service {listViewItem.Text} could not be started. Did you run the program with the Administrator privilege ?",
                        "Lemoine Service Monitoring",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
      }
      this.Invoke (new MethodInvoker (progressBar.PerformStep));
      await UpdateStatusAsync (listViewItem, sc);
    }

    async Task StartServicesAsync (IEnumerable<WindowsServiceController> serviceControllers)
    {
      var tasks = serviceControllers
        .Select (sc => sc.StartServiceAsync ());
      await RunTasks (tasks);
    }

    async Task StartServiceAsync (ListViewItem listViewItem, WindowsServiceController sc)
    {
      switch (sc.Status) {
      case ServiceControllerStatus.Stopped:
      case ServiceControllerStatus.Paused:
        InvokeSetStatus (listViewItem, GetTranslation ("Starting"), sc.StartMode);
        break;
      default:
        break;
      }
      await sc.StartServiceAsync ();
      await UpdateStatusAsync (listViewItem, sc);
    }

    /// <summary>
    /// Stop the watching service and returns true if it was running,
    /// else return false
    /// </summary>
    /// <returns></returns>
    async Task<bool> StopWatchingServiceAsync ()
    {
      var controlSC = new WindowsServiceController ("Lem_WatchDogService");
      try {
        await controlSC.StopServiceAsync ();
        return true;
      }
      catch (Exception ex) {
        log.Error ($"StopWatchingService: the watching service is probably not installed, give up", ex);
        return false;
      }
    }

    async Task StartWatchingServiceAsync ()
    {
      var controlSC = new WindowsServiceController ("Lem_WatchDogService");
      try {
        await controlSC.StartServiceAsync ();
      }
      catch (Exception ex) {
        log.Error ("StartWatchingService: the watching service is probably not installed, => give up ", ex);
        return;
      }
    }

    private async Task AddServiceAsync (string serviceName, int iGroup)
    {
      await AddServiceAsync (serviceName, "", iGroup);
    }


    private async Task AddServiceAsync (string serviceName, string hostName, int iGroup)
    {
      log.Debug ($"AddServiceAsync: serviceName={serviceName} on {hostName}");

      //
      // add it to listView
      //
      try {
        WindowsServiceController sc;
        if (hostName.Length == 0) {
          sc = new WindowsServiceController (serviceName);
        }
        else {
          sc = new WindowsServiceController (serviceName, hostName);
        }

        string status;
        int pid = 0;
        if (!(await sc.CheckPingAsync ())) {
          log.Warn ($"AddServiceAsync: skip service {serviceName} from host {hostName} because ping is not ok");
          return;
        }
        else {
          ServiceControllerStatus serviceStatus;
          try {
            serviceStatus = sc.Status;
            status = "";
            switch (sc.Status) {
            case ServiceControllerStatus.Running:
              status = "Started";
              try {
                status = PulseCatalog.GetString ("Started");
              }
              catch (Exception) {
              }
              if (iGroup == 0) {
                pid = sc.GetServicePID ();
              }

              break;
            default:
              break;
            }
          }
          catch (Exception ex) {
            log.Info ($"AddServiceAsync: exception in Status, skip service {serviceName} because it may not exist", ex);
            return;
          }
        }
        //

        if (1 == Interlocked.Increment (ref m_listViewUpdate)) {
          this.SuspendLayout ();
        }
        ListViewItem serviceItem = new ListViewItem (new string[] {
          serviceName,
          sc.MachineName,
          status,
          sc.StartMode,
          sc.DisplayName,
          pid == 0 ? "" : pid.ToString()
                                                    });
        serviceItem.Group = listView.Groups[iGroup];
        listView.Items.Add (serviceItem);

      }
      catch (Exception) {
        // The service is unknown: do nothing
      }
      finally {
        // MainForm
        //
        if (0 == Interlocked.Decrement (ref m_listViewUpdate)) {
          this.ResumeLayout (false);
        }
      }
    }

    IEnumerable<ListViewItem> GetSelectedItems ()
    {
      var selectedItems = listView.SelectedItems;
      for (int i = 0; i < selectedItems.Count; ++i) {
        yield return selectedItems[i];
      }
    }

    IEnumerable<ListViewItem> GetAllItems ()
    {
      var items = listView.Items;
      for (int i = 0; i < items.Count; ++i) {
        yield return items[i];
      }
    }

    WindowsServiceController GetServiceController (ListViewItem listViewItem)
    {
      try {
        return new WindowsServiceController (listViewItem.Text, listViewItem.SubItems[1].Text);
      }
      catch (Exception) {
        throw;
      }
    }

    async void itemSelectionChanged (Object sender, ListViewItemSelectionChangedEventArgs e)
    {
      progressBar.Value = 0;

      var selectedItems = GetSelectedItems ();
      var itemServiceControllers = selectedItems
        .Select (x => (x, GetServiceController (x)));
      foreach (var itemServiceController in itemServiceControllers) {
        await UpdateStatusAsync (itemServiceController.Item1, itemServiceController.Item2);
      }
      UpdateButtons (itemServiceControllers.Select (x => x.Item2));
    }

    void InvokeUpdateButtons (params WindowsServiceController[] serviceControllers)
    {
      Invoke (new MethodInvoker (() => UpdateButtons (serviceControllers)));
    }

    void InvokeUpdateButtons (IEnumerable<WindowsServiceController> serviceControllers)
    {
      Invoke (new MethodInvoker (() => UpdateButtons (serviceControllers)));
    }

    void UpdateButtons (IEnumerable<WindowsServiceController> serviceControllers)
    {
      if (!serviceControllers.Any ()) {
        return;
      }

      try {
        var sameStartupType = serviceControllers
          .Select (x => x.StartupType)
          .Same ();
        autoToolStripMenuItem.Checked = serviceControllers
          .All (x => x.StartupType.Equals (StartupType.Auto));
        autoToolStripMenuItem.Enabled = !autoToolStripMenuItem.Checked
          && sameStartupType;
        manualToolStripMenuItem.Checked = serviceControllers
          .All (x => x.StartupType.Equals (StartupType.Manual));
        manualToolStripMenuItem.Enabled = !manualToolStripMenuItem.Checked
          && sameStartupType;
        disabledToolStripMenuItem.Checked = serviceControllers
          .All (x => x.StartupType.Equals (StartupType.Disabled));
        disabledToolStripMenuItem.Enabled = !disabledToolStripMenuItem.Checked
          && sameStartupType;

        var sameStatus = serviceControllers
          .Select (x => x.Status)
          .Same ();
        if (sameStatus) {
          var sc = serviceControllers.First ();
          switch (sc.Status) {
          case ServiceControllerStatus.Running:
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
            buttonRestart.Enabled = true;
            startToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = true;
            restartToolStripMenuItem.Enabled = true;
            break;
          case ServiceControllerStatus.ContinuePending:
          case ServiceControllerStatus.StartPending:
            buttonStart.Enabled = false;
            buttonStop.Enabled = false;
            buttonRestart.Enabled = false;
            startToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = false;
            restartToolStripMenuItem.Enabled = false;
            break;
          case ServiceControllerStatus.Stopped:
          default:
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            buttonRestart.Enabled = false;
            startToolStripMenuItem.Enabled = true;
            stopToolStripMenuItem.Enabled = false;
            restartToolStripMenuItem.Enabled = false;
            break;
          }
        }
        else { // !sameStatus 
          buttonStart.Enabled = false;
          buttonStop.Enabled = false;
          buttonRestart.Enabled = false;
          startToolStripMenuItem.Enabled = false;
          stopToolStripMenuItem.Enabled = false;
          restartToolStripMenuItem.Enabled = false;
          autoToolStripMenuItem.Checked = false;
          manualToolStripMenuItem.Checked = false;
          disabledToolStripMenuItem.Checked = false;
          autoToolStripMenuItem.Enabled = false;
          manualToolStripMenuItem.Enabled = false;
          disabledToolStripMenuItem.Enabled = false;
        }
      }
      catch (Exception) {
        buttonStart.Enabled = false;
        buttonStop.Enabled = false;
        buttonRestart.Enabled = false;
        startToolStripMenuItem.Enabled = false;
        stopToolStripMenuItem.Enabled = false;
        restartToolStripMenuItem.Enabled = false;
        autoToolStripMenuItem.Checked = false;
        manualToolStripMenuItem.Checked = false;
        disabledToolStripMenuItem.Checked = false;
        autoToolStripMenuItem.Enabled = false;
        manualToolStripMenuItem.Enabled = false;
        disabledToolStripMenuItem.Enabled = false;
      }
    }

    void InvokeSetStatus (ListViewItem listViewItem, string text, string startupType = "", int pid = 0)
    {
      Invoke (new MethodInvoker (() => SetStatusText (listViewItem, text, startupType, pid)));
    }

    void SetStatusText (ListViewItem listViewItem, string text, string startupType = "", int pid = 0)
    {
      listViewItem.SubItems[2].Text = text;
      if (!string.IsNullOrEmpty (startupType)) {
        listViewItem.SubItems[3].Text = startupType;
      }
      if (0 == pid) {
        listViewItem.SubItems[5].Text = "";
      }
      else {
        listViewItem.SubItems[5].Text = pid.ToString ();
      }
    }

    async Task UpdateStatusAsync (ListViewItem listViewItem, WindowsServiceController sc)
    {
      try {
        if (!(await sc.CheckPingAsync ())) {
          InvokeSetStatus (listViewItem, "Ping ko");
          return;
        }

        switch (sc.Status) {
        case ServiceControllerStatus.Running:
          // List status value
          int PID = 0;
          if (object.Equals (listViewItem.Group.Name, "LemoineGroup")) {
            PID = sc.GetServicePID ();
          }
          InvokeSetStatus (listViewItem, GetTranslation ("Started"), startupType: sc.StartMode, pid: PID);
          break;
        case ServiceControllerStatus.ContinuePending:
        case ServiceControllerStatus.StartPending:
          InvokeSetStatus (listViewItem, GetTranslation ("Starting"), sc.StartMode);
          break;
        case ServiceControllerStatus.StopPending:
        case ServiceControllerStatus.PausePending:
          InvokeSetStatus (listViewItem, GetTranslation ("Stopping"), sc.StartMode);
          break;
        case ServiceControllerStatus.Stopped:
        default:
          InvokeSetStatus (listViewItem, "", sc.StartMode);
          break;
        }
      }
      catch (Exception ex) {
        log.Error ($"UpdateStatusAsync: exception", ex);
      }
    }

    async void buttonStartClick (object sender, EventArgs e)
    {
      var numberServices = listView.SelectedItems.Count;
      progressBar.Value = 0;
      progressBar.Maximum = numberServices + 1;

      await StartSelectedServicesAsync (listView);
      await RefreshAllStatusAsync ();

      // - If all the auto services are running except the watching service,
      //   restart it
      bool allAutoServicesRunning = true;
      foreach (ListViewItem i in listView.Items) {
        if (!object.Equals (i.Group.Name, "LemoineGroup")) {
          // Consider only the services in the Lemoine group here
          continue;
        }
        if (i.Text.Equals ("Lem_WatchDogService")
          || i.Text.Contains ("Lemoine WatchDog Service")
          || i.Text.Equals ("Lem_Control")
          || i.Text.Contains ("Lemoine Watchdog Service")
          || i.Text.Contains ("Lemoine Watching Service")) {
          // Skip the watching service
          continue;
        }
        WindowsServiceController sc = new WindowsServiceController (i.Text,
                                                         i.SubItems[1].Text);
        if (!sc.StartupType.Equals (StartupType.Auto)) {
          continue;
        }
        if (sc.Status != ServiceControllerStatus.Running) {
          if (log.IsDebugEnabled) {
            log.Debug ($"buttonStartClick: {i.Text} is not running");
          }
          allAutoServicesRunning = false;
          break;
        }
      }
      if (allAutoServicesRunning) {
        await StartWatchingServiceAsync ();
        progressBar.Value = progressBar.Maximum;
        await RefreshAllStatusAsync ();
      }
      progressBar.Value = progressBar.Maximum;
    }

    async void buttonStopClick (object sender, EventArgs e)
    {
      var numberServices = listView.SelectedItems.Count;
      progressBar.Value = 0;
      progressBar.Maximum = numberServices + 1;

      await StopWatchingServiceAsync ();
      this.Invoke (new MethodInvoker (progressBar.PerformStep));

      await StopSelectedServicesAsync (listView);

      progressBar.Value = progressBar.Maximum;
      await RefreshAllStatusAsync ();
    }

    async void buttonRestartClick (object sender, EventArgs e)
    {
      var numberServices = listView.SelectedItems.Count;
      progressBar.Value = 0;
      progressBar.Maximum = 2 * numberServices + 2;

      // 1. Begin to stop Lem_WatchDogService if needed
      bool restartWatchingService = await StopWatchingServiceAsync ();
      this.Invoke (new MethodInvoker (progressBar.PerformStep));

      await StopSelectedServicesAsync (listView);
      await StartSelectedServicesAsync (listView);

      // 3. Restart Lem_WatchDogService if needed
      if (restartWatchingService == true) {
        await StartWatchingServiceAsync ();
      }
      progressBar.Value = progressBar.Maximum;

      // 4. Refresh the display
      await RefreshAllStatusAsync ();
    }

    async void buttonAutoClick (object sender, EventArgs e)
    {
      foreach (ListViewItem i in listView.SelectedItems) {
        progressBar.Value = 0;
        var sc = new WindowsServiceController (i.Text,
                                                         i.SubItems[1].Text);
        try {
          sc.StartupType = StartupType.Auto;
        }
        catch (Exception) {
          // raise a message
          MessageBox.Show ("The startup type of the service could not be updated. " +
                          "Did you run the program with the Administrator privilege ?",
                          "Lemoine Service Monitoring",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        await UpdateStatusAsync (i, sc);
      }
      return;
    }

    async void buttonManualClick (object sender, EventArgs e)
    {
      foreach (ListViewItem i in listView.SelectedItems) {
        progressBar.Value = 0;
        var sc = new WindowsServiceController (i.Text,
                                                         i.SubItems[1].Text);
        try {
          sc.StartupType = StartupType.Manual;
        }
        catch (Exception) {
          // raise a message
          MessageBox.Show ("The startup type of the service could not be updated. " +
                          "Did you run the program with the Administrator privilege ?",
                          "Lemoine Service Monitoring",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        await UpdateStatusAsync (i, sc);
      }
      return;
    }

    async void buttonDisabledClick (object sender, EventArgs e)
    {
      foreach (ListViewItem i in listView.SelectedItems) {
        progressBar.Value = 0;
        WindowsServiceController sc = new WindowsServiceController (i.Text,
                                                         i.SubItems[1].Text);
        try {
          sc.StartupType = StartupType.Disabled;
        }
        catch (Exception) {
          // raise a message
          MessageBox.Show ("The startup type of the service could not be updated. " +
                          "Did you run the program with the Administrator privilege ?",
                          "Lemoine Service Monitoring",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        await UpdateStatusAsync (i, sc);
      }
    }

    async void buttonRefreshClick (object sender, EventArgs e)
    {
      await RefreshAllStatusAsync ();
    }

    async Task RefreshAllStatusAsync ()
    {
      var items = GetAllItems ();
      var itemServiceControllers = items
        .Select (x => (x, GetServiceController (x)));
      foreach (var itemServiceController in itemServiceControllers) {
        await UpdateStatusAsync (itemServiceController.Item1, itemServiceController.Item2);
      }
      var selectedServiceControllers = itemServiceControllers
        .Where (x => x.Item1.Selected)
        .Select (x => x.Item2);
      InvokeUpdateButtons (selectedServiceControllers);
    }
    #endregion

    async void ButtonStopAllClick (object sender, EventArgs e)
    {
      // Consider only the services in the Lemoine group here
      var listViewItems = new List<ListViewItem> ();
      foreach (ListViewItem i in listView.Items) {
        if (!object.Equals (i.Group.Name, "LemoineGroup")) {
          // Consider only the services in the Lemoine group here
          continue;
        }
        if (i.Text.Equals ("Lem_WatchDogService")
          || i.Text.Equals ("Lem_Control")) {
          // Skip the watching service for the moment,
          // it will be started only once all the services are started
          continue;
        }
        listViewItems.Add (i);
      }

      var numberServices = listViewItems.Count + 1;
      progressBar.Value = 0;
      progressBar.Maximum = numberServices;

      await StopWatchingServiceAsync ();

      await StopServicesAsync (listViewItems);

      progressBar.Value = progressBar.Maximum;
      await RefreshAllStatusAsync ();
    }

    void ContextMenuStripOpening (object sender, System.ComponentModel.CancelEventArgs e)
    {
      ListViewItem sel = listView.FocusedItem;
      if (sel == null) {
        return;
      }

      dependenciesToolStripMenuItem.DropDownItems.Clear ();
      dependsOnToolStripMenuItem.DropDownItems.Clear ();
      try {
        var sc = new WindowsServiceController (sel.Text, supportDependencies: true);
        if (sc is null) {
          return;
        }

        foreach (string svc in sc.DependentServicesNames) {
          dependenciesToolStripMenuItem.DropDownItems.Add (svc);
        }

        foreach (string svc in sc.DependsOnServicesNames) {
          dependsOnToolStripMenuItem.DropDownItems.Add (svc);
        }
      }
      catch (Exception ex) {
        log.Error ($"ContextMenuStripOpening: problem getting the dependent services", ex);
      }
    }

    async void ButtonStartAllClick (object sender, EventArgs e)
    {
      // Consider only the services in the Lemoine group here
      var listViewItems = new List<ListViewItem> ();
      foreach (ListViewItem i in listView.Items) {
        if (!object.Equals (i.Group.Name, "LemoineGroup")) {
          // Consider only the services in the Lemoine group here
          continue;
        }
        if (i.Text.Equals ("Lem_WatchDogService")
          || i.Text.Equals ("Lem_Control")) {
          // Skip the watching service for the moment,
          // it will be started only once all the services are started
          continue;
        }
        listViewItems.Add (i);
      }

      var numberServices = listViewItems.Count + 1;
      progressBar.Value = 0;
      progressBar.Maximum = numberServices;

      await StartServicesAsync (listViewItems, autoOnly: true);

      await StartWatchingServiceAsync ();

      progressBar.Value = progressBar.Maximum;
      await RefreshAllStatusAsync ();
    }

    string GetTranslation (string key)
    {
      try {
        return PulseCatalog.GetString (key);
      }
      catch (Exception) {
        return key;
      }
    }

    string GetTranslation (string key, string defaultTranslation)
    {
      try {
        return PulseCatalog.GetString (key, defaultTranslation);
      }
      catch (Exception) {
        return defaultTranslation;
      }
    }

    private void MainForm_FormClosed (object sender, FormClosedEventArgs e)
    {
      Application.Exit ();
    }
  }
}
