// Copyright (C) 2009-2023 Lemoine Automation Technologies
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

#if NETSTANDARD || NET48 || NETCOREAPP
using Lemoine.Cnc.DataRepository;
#endif // NETSTANDARD || NET48 || NETCOREAPP
using Lemoine.DataRepository;
using Lemoine.Info;
using Lemoine.Model;
using Lemoine.Threading;
using Lemoine.Core.Log;
#if NETSTANDARD || NET48 || NETCOREAPP
using Lemoine.Extensions.Interfaces;
#endif // NETSTANDARD || NET48 || NETCOREAPP
using Lemoine.Core.Plugin;
using Lemoine.FileRepository;

namespace Lemoine.CncEngine
{
  /// <summary>
  /// Main acquisition class to be run in a thread or in a class
  /// </summary>
  public class Acquisition: ProcessOrThreadClass, IThreadClass, IProcessClass
  {
    #region Members
#if NETSTANDARD || NET48 || NETCOREAPP
    readonly IExtensionsLoader m_extensionsLoader;
#endif // NETSTANDARD || NET48 || NETCOREAPP
    readonly IAssemblyLoader m_assemblyLoader;
    readonly IFileRepoClientFactory m_fileRepoClientFactory;
    readonly int m_cncAcquisitionId;
    volatile CncDataHandler m_cncDataHandler = null;
    readonly TimeSpan m_every = TimeSpan.FromSeconds (2);
    readonly bool m_useStaThread = false;
    readonly bool m_useProcess = false;
    readonly Repository m_repository;
    AcquisitionProcessExecution m_processExecution = null;
#endregion // Members

    ILog log = LogManager.GetLogger(typeof (Acquisition).FullName);

#region Getters / Setters
    /// <summary>
    /// CncAcquisitionId
    /// </summary>
    public int CncAcquisitionId {
      get { return m_cncAcquisitionId; }
    }
    
    /// <summary>
    /// Frequency when the data is acquired
    /// </summary>
    public TimeSpan Every {
      get { return m_every; }
    }
    
    /// <summary>
    /// Use an Sta thread (from ICncAcquisition)
    /// </summary>
    public bool UseStaThread {
      get { return m_useStaThread; }
    }

    /// <summary>
    /// Use a process (from ICncAcquisition)
    /// </summary>
    public bool UseProcess {
      get { return m_useProcess; }
    }

    /// <summary>
    /// Latest execution date/time of the method
    /// </summary>
    public override DateTime LastExecution {
      get
      {
        if (null == m_cncDataHandler) {
          log.DebugFormat ("LastExecution.get: " +
                           "Cnc data handler is not set yet " +
                           "=> fallback to base.LastExecution");
          return base.LastExecution;
        }
        else {
          return m_cncDataHandler.LastExecution;
        }
      }
      set
      {
        if (null == m_cncDataHandler) {
          log.DebugFormat ("LastExecution.set: " +
                           "Cnc data handler is not set yet " +
                           "=> fallback to base.LastExecution");
          base.LastExecution = value;
        }
        else {
          m_cncDataHandler.LastExecution = value;
        }
      }
    }
    
    /// <summary>
    /// Reference to the CNC Data Handler
    /// </summary>
    public CncDataHandler CncDataHandler {
      get { return m_cncDataHandler; }
    }

    /// <summary>
    /// Final data once all the module requests are completed
    /// 
    /// Thread safe access (a concurrent dictionary is used)
    /// 
    /// This is possible that from time to time a deprecated value is returned,
    /// but this should be pretty rare (there is no global lock)
    /// 
    /// null if the cnc data handler is not initialized yet
    /// </summary>
    public IDictionary<string, object> FinalData {
    get {
        if (null == m_cncDataHandler) {
          log.Error ($"FinalData.get: cncDataHandler is not initialized yet => return null");
          return null;
        }
        return m_cncDataHandler.FinalData;
      }
    }
    #endregion // Getters / Setters

    #region Constructors
#if NETSTANDARD || NET48 || NETCOREAPP
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="extensionsLoader">not null</param>
    /// <param name="cncAcquisition">not null</param>
    /// <param name="assemblyLoader"></param>
    /// <param name="fileRepoClientFactory"></param>
    public Acquisition (IExtensionsLoader extensionsLoader, ICncAcquisition cncAcquisition, IAssemblyLoader assemblyLoader, IFileRepoClientFactory fileRepoClientFactory)
    {
      Debug.Assert (null != extensionsLoader);
      Debug.Assert (null != cncAcquisition);

      m_extensionsLoader = extensionsLoader;
      m_cncAcquisitionId = cncAcquisition.Id;
      m_assemblyLoader = assemblyLoader ?? AssemblyLoaderProvider.AssemblyLoader;
      m_fileRepoClientFactory = fileRepoClientFactory;
      m_every = cncAcquisition.Every;
      m_useStaThread = cncAcquisition.StaThread;
      m_useProcess = cncAcquisition.UseProcess;
      m_repository = CreateRepositoryFromCncAcquisition (cncAcquisition.Id);
      
      log = LogManager.GetLogger(string.Format ("{0}.{1}",
                                                typeof (Acquisition).FullName,
                                                cncAcquisition.Id));
      
      // Set the parent process Id
      try {
        base.ParentProcessId = Process.GetCurrentProcess().Id;
      }
      catch (Exception ex) {
        log.Error ($"Acquisition: error while getting the parent process Id", ex);
      }
    }

    /// <summary>
    /// Alternative constructor
    /// </summary>
    /// <param name="extensionsLoader">not null</param>
    /// <param name="cncAcquisitionId"></param>
    /// <param name="every"></param>
    /// <param name="assemblyLoader"></param>
    /// <param name="fileRepoClientFactory"></param>
    /// <param name="useStaThread"></param>
    /// <param name="useProcess">use process</param>
    public Acquisition (IExtensionsLoader extensionsLoader, int cncAcquisitionId, TimeSpan every, IAssemblyLoader assemblyLoader, IFileRepoClientFactory fileRepoClientFactory, bool useStaThread = false, bool useProcess = false)
    {
      m_extensionsLoader = extensionsLoader;
      m_cncAcquisitionId = cncAcquisitionId;
      m_assemblyLoader = assemblyLoader ?? AssemblyLoaderProvider.AssemblyLoader;
      m_fileRepoClientFactory = fileRepoClientFactory;
      m_every = every;
      m_useStaThread = useStaThread;
      m_useProcess = useProcess;
      m_repository = CreateRepositoryFromCncAcquisition (cncAcquisitionId);

      log = LogManager.GetLogger(string.Format ("{0}.{1}",
                                                typeof (Acquisition).FullName,
                                                cncAcquisitionId));
    }
#endif // NETSTANDARD || NET48 || NETCOREAPP

    /// <summary>
    /// Acquisition using a local file
    /// </summary>
    /// <param name="localFilePath"></param>
    /// <param name="useProcess"></param>
    /// <param name="assemblyLoader"></param>
    /// <param name="fileRepoClientFactory"></param>
    public Acquisition (string localFilePath, IAssemblyLoader assemblyLoader, IFileRepoClientFactory fileRepoClientFactory, bool useProcess = false)
    {
      m_cncAcquisitionId = 0;
      m_repository = CreateRepositoryFromLocalFile (localFilePath);
      m_assemblyLoader = assemblyLoader;
      m_fileRepoClientFactory = fileRepoClientFactory;
      m_useProcess = useProcess;
    }

#if NETSTANDARD || NET48 || NETCOREAPP
    /// <summary>
    /// Acquisition using a <see cref="Repository"/>
    /// </summary>
    /// <param name="cncAcquisitionId"></param>
    /// <param name="repository"></param>
    /// <param name="assemblyLoader"></param>
    /// <param name="fileRepoClientFactory"></param>
    public Acquisition (int cncAcquisitionId, Repository repository, IAssemblyLoader assemblyLoader, IFileRepoClientFactory fileRepoClientFactory)
    {
      Debug.Assert (null != repository);

      m_cncAcquisitionId = cncAcquisitionId;
      m_repository = repository;
      m_assemblyLoader = assemblyLoader;
      m_fileRepoClientFactory = fileRepoClientFactory;
    }
#endif // NETSTANDARD || NET48 || NETCOREAPP
    #endregion // Constructors

    #region Methods

    /// <summary>
    /// Get a module that is associated to specific reference
    /// </summary>
    /// <param name="moduleRef"></param>
    /// <returns>null if the cnc data handler is not initialized yet</returns>
    public CncModuleExecutor GetModule (string moduleRef)
    {
      if (null == m_cncDataHandler) {
        log.Error ($"GetModule: cncDataHandler is not initialized yet, moduleRef={moduleRef} => return null");
        return null;
      }
      return m_cncDataHandler.GetModule (moduleRef);
    }

    /// <summary>
    /// Process a specific module given its module XML Element and its <see cref="CncModuleExecutor"/>
    /// 
    /// Support only the element with tag moduleref for the moment
    /// </summary>
    /// <param name="moduleElement"></param>
    /// <param name="data">not null</param>
    /// <param name="cancellationToken"></param>
    public void ProcessModule (System.Xml.XmlElement moduleElement, IDictionary<string, object> data, CancellationToken cancellationToken)
    {
      Debug.Assert (null != data);

      if (null == m_cncDataHandler) {
        log.Error ($"ProcessModule: cncDataHandler is not initialized yet => throw an exception");
        throw new Exception ("Cnc data handler is not initialized yet");
      }

      if (moduleElement.Name.Equals ("moduleref")) {
        var cncModuleExecutor = GetModule (moduleElement.GetAttribute ("ref"));
        m_cncDataHandler.ProcessModule (moduleElement, cncModuleExecutor, data, cancellationToken);
      }
      else { // Not supported
        log.Error ($"ProcessModule: element with name {moduleElement.Name} is not supported");
        throw new NotSupportedException ($"Element {moduleElement.Name} is not supported");
      }
    }

#if NETSTANDARD || NET48 || NETCOREAPP
    Repository CreateRepositoryFromCncAcquisition (int cncAcquisitionId)
    {
      var cncDirectory = Path.Combine (PulseInfo.LocalConfigurationDirectory, "Cnc");
      var factory = new CncFileRepoFactory (m_extensionsLoader, m_cncAcquisitionId, this);
      var localConfigurationFile = $"CncAcquisition-{m_cncAcquisitionId}.xml";
      if (Directory.Exists (cncDirectory)) {
        localConfigurationFile = Path.Combine (cncDirectory,
                                               localConfigurationFile);
      }
      else {
        localConfigurationFile = Path.Combine (PulseInfo.LocalConfigurationDirectory,
                                               localConfigurationFile);
      }
      if (log.IsDebugEnabled) {
        log.Debug ($"CreateRepositoryFromCncAcquisition: localConfigurationFile is {localConfigurationFile}");
      }
      try {
        var copyFactory = new XMLFactory (XmlSourceType.URI, localConfigurationFile);
        var copyBuilder = new XMLBuilder (localConfigurationFile);
        log.Debug ("CreateRepositoryFromCncAcquisition: copy factory and builder created");
        return new Repository (factory, copyBuilder, copyFactory);
      }
      catch (Exception ex) {
        log.Error ("CreateRepositoryFromCncAcquisition: new Repository failed", ex);
        throw;
      }
    }
#endif // NETSTANDARD || NET48 || NETCOREAPP

    Repository CreateRepositoryFromLocalFile (string localFilePath)
    {
      var factory = new XMLFactory (XmlSourceType.URI, localFilePath);
      return new Repository (factory);
    }

    /// <summary>
    /// Initialize the CNC data handler
    /// 
    /// It is already called by the Run method, so normally (except in Lem_CncGUI)
    /// it should not be called manually
    /// </summary>
    /// <returns>success</returns>
    public bool InitDataHandler (CancellationToken cancellationToken)
    {
      try {
        m_cncDataHandler = new CncDataHandler (m_repository, m_assemblyLoader, m_fileRepoClientFactory, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested ();
      }
      catch (ConfigReloadRequired) {
        log.Info ("InitDataHandler: ConfigReloadRequired was returned, return false");
        return false;
      }
      catch (Exception ex) {
        log.Error ("InitDataHandler: new CncDataHandler failed", ex);
        throw;
      }
      Debug.Assert (null != m_cncDataHandler);
      m_cncDataHandler.Every = m_every;
      if (log.IsDebugEnabled) {
        log.Debug ($"InitDataHandler: successfully initialized with every={m_every}");
      }
      return true;
    }
#endregion // Methods
    
#region Implementation of ProcessOrThreadClass
    /// <summary>
    /// Main thread method
    /// 
    /// Implements <see cref="ProcessOrThreadClass">ProcessOrThreadClass</see>
    /// </summary>
    protected override void Run (System.Threading.CancellationToken cancellationToken)
    {
      try {
        SetActive ();
        if (null != m_cncDataHandler) {
          log.Warn ("Run: reset the data handler");
          m_cncDataHandler.Dispose ();
          m_cncDataHandler = null;
        }
        SetActive ();
        while (!InitDataHandler (cancellationToken) && !cancellationToken.IsCancellationRequested) {
          // Try to reload once the configuration
          SetActive ();
          log.Info ($"Run: the data handler is not initialized, retry in 1s");
          this.Sleep (TimeSpan.FromSeconds (1), cancellationToken);
        }
        SetActive ();
        if (!cancellationToken.IsCancellationRequested) {
          m_cncDataHandler.Work (this.UseStampFile, this.ParentProcessId, cancellationToken);
      }
      }
      catch (Exception ex) {
        log.Fatal ("Run: exception", ex);
      }

      if (cancellationToken.IsCancellationRequested) {
        log.Error ("Run: cancellation was requested");
      }
    }
    
    /// <summary>
    /// Given the Cnc Acquisition ID, get the stamp file to use
    /// to check a process
    ///
    /// Implements <see cref="ProcessOrThreadClass">ProcessOrThreadClass</see>
    /// </summary>
    /// <returns></returns>
    public override string GetStampFileName ()
    {
      if (null == m_cncDataHandler) { // Fallback
        return $"CncStamp-{m_cncAcquisitionId}";
      }
      else {
        return m_cncDataHandler.GetStampFileName ();
      }
    }
    
    /// <summary>
    /// Logger
    /// 
    /// Implements <see cref="ProcessOrThreadClass">ProcessOrThreadClass</see>
    /// </summary>
    /// <returns></returns>
    public override ILog GetLogger()
    {
      return log;
    }
#endregion // Implementation of ProcessOrThreadClass

    /// <summary>
    /// Start the acquisition in a thread if UseProcess is false, else in a process
    /// </summary>
    public void StartThreadOrProcess (CancellationToken cancellationToken)
    {
      if (log.IsDebugEnabled) {
        log.Debug ($"StartThreadOrProcess: start UseProcess={this.UseProcess}");
  }

      try {
        if (this.UseProcess) {
          m_processExecution = new AcquisitionProcessExecution (this);
          m_processExecution.Start ();
}
        else {
          this.Start (cancellationToken, this.UseStaThread ? ApartmentState.STA : ApartmentState.MTA);
        }
        if (log.IsDebugEnabled) {
          log.Debug ("StartThreadOrProcess: successfully started");
        }
      }
      catch (Exception ex) {
        log.Error ("StartThreadOrProcess: error while trying to start the acquisition", ex);
      }
    }

    /// <summary>
    /// Add the acquisition to a <see cref="CheckThreadsAndProcesses"/>
    /// </summary>
    /// <param name="threadsAndProcessesChecker"></param>
    public void AddToThreadsAndProcessesChecker (CheckThreadsAndProcesses threadsAndProcessesChecker)
    {
      if (null != m_processExecution) {
        threadsAndProcessesChecker.AddProcess (m_processExecution);
      }
      else {
        threadsAndProcessesChecker.AddThread (this);
      }
    }
  }
}
