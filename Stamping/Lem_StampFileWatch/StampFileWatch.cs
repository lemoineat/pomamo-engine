// Copyright (C) 2009-2023 Lemoine Automation Technologies
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using Lemoine.Core.Log;
using Lemoine.Threading;

namespace Lemoine.Stamping.Lem_StampFileWatch
{
  /// <summary>
  /// Main class of service Lem_StampFileWatch
  /// </summary>
  public sealed class StampFileWatch
    : Lemoine.Threading.IThreadService, IDisposable
  {
    static readonly TimeSpan NOT_RESPONDING_TIMEOUT_KEY = TimeSpan.FromMinutes (5);

    #region Members
    DirectoryManager m_directoryManager;
    readonly CheckThreadsAndProcesses m_checkThreads = new CheckThreadsAndProcesses ();
    readonly CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource ();
    bool m_disposed = false;
    #endregion

    static readonly ILog log = LogManager.GetLogger (typeof (StampFileWatch).FullName);

    #region Constructors
    /// <summary>
    /// Constructor
    /// </summary>
    public StampFileWatch ()
    {
    }
    #endregion // Constructors

    #region Methods
    /// <summary>
    /// <see cref="IThreadService"/>
    /// </summary>
    public void Initialize ()
    {
      log.DebugFormat ("StampFileWatch: Initialize");
      try {
        InitializeThreads (CancellationToken.None);
      }
      catch (Exception ex) {
        log.Error ($"InitializeAsync: exception", ex);
        throw;
      }
    }

    void InitializeThreads (CancellationToken cancellationToken)
    {
      using (var linkedCancellationTokenSource = CancellationTokenSource
        .CreateLinkedTokenSource (m_cancellationTokenSource.Token, cancellationToken)) {
        var linkedToken = linkedCancellationTokenSource.Token;

        // thread to monitor the index files directory
        m_directoryManager = new DirectoryManager ();
        m_directoryManager.Start (linkedToken); // TODO: cancellation token

        // - Create the thread that checks the other threads
        m_checkThreads.AddThread (m_directoryManager);
        m_checkThreads.NotRespondingTimeout = NOT_RESPONDING_TIMEOUT_KEY;
        m_checkThreads.Start (linkedToken);

        // - Check no 'exit' was requested
        while (!m_directoryManager.ExitRequested) {
          linkedToken.WaitHandle.WaitOne (100);
          if (linkedToken.IsCancellationRequested) {
            log.Info ($"InitializeThreads: cancellation requested (OnStop called), return");
            return;
          }
        }
      }

      log.Fatal ("InitializeThreads: exit was requested");
      try {
        m_checkThreads.Abort ();
        log.Fatal ("InitializeThreads: checkThreads aborted because Exit was requested");
        m_directoryManager.Abort ();
        log.Fatal ("InitializeThreads: m_directoryManager aborted because Exit was requested");
      }
      finally {
        log.Fatal ("InitializeThreads: exit requested. Skip");
        //Lemoine.Core.Environment.LogAndForceExit (log);
      }
    }

    /// <summary>
    /// Stop this service.
    /// </summary>
    public void OnStop ()
    {
      m_cancellationTokenSource?.Cancel ();

      m_checkThreads?.Abort ();
      m_directoryManager?.Abort ();
    }
    #endregion Methods

    #region IDisposable implementation
    /// <summary>
    /// <see cref="IDisposable"/>
    /// </summary>
    ~StampFileWatch () => Dispose (false);

    /// <summary>
    /// <see cref="IDisposable"/>
    /// </summary>
    public void Dispose ()
    {
      Dispose (true);
      GC.SuppressFinalize (this);
    }

    /// <summary>
    /// <see cref="IDisposable"/>
    /// </summary>
    /// <param name="disposing"></param>
    void Dispose (bool disposing)
    {
      if (m_disposed) {
        return;
      }

      if (disposing) {
        m_checkThreads?.Dispose ();
        m_cancellationTokenSource?.Dispose ();
      }

      m_disposed = true;
    }
    #endregion // IDisposable implementation
  }
}
