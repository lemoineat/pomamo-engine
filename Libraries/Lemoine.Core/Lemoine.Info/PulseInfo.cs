// Copyright (C) 2009-2023 Lemoine Automation Technologies
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Lemoine.Core.Log;
using static System.Environment;

namespace Lemoine.Info
{
  /// <summary>
  /// Get information from the registry
  /// (global settings set during the installation of the system)
  /// </summary>
  public sealed class PulseInfo
  {
    static readonly string WEB_SERVICE_URL_KEY = "WebServiceUrl";
    static readonly string WEB_SERVICE_URL_DEFAULT = "";

    static readonly string MAIN_WEB_SERVICE_URL_KEY = "MainWebServiceUrl";
    static readonly string MAIN_WEB_SERVICE_URL_DEFAULT = "";

    static readonly string FILE_REPOSITORY_CLIENT_KEY = "FileRepoClient"; // web / corba / directory
    static readonly string FILE_REPOSITORY_CLIENT_WEB = "web";
    static readonly string FILE_REPOSITORY_CLIENT_CORBA = "corba";
    static readonly string FILE_REPOSITORY_CLIENT_DIRECTORY = "directory";
    static readonly string FILE_REPOSITORY_CLIENT_MULTI = "multi";
    static readonly string FILE_REPOSITORY_CLIENT_DEFAULT = FILE_REPOSITORY_CLIENT_MULTI;

    static readonly string SHARED_DIRECTORY_PATH_KEY = "SharedDirectoryPath";
    static readonly string SHARED_DIRECTORY_PATH_DEFAULT = "";

    static readonly string INSTALLATION_DIR_KEY = "InstallDir";
    static readonly string PULSE_SERVER_INSTALL_DIR_KEY = "PulseServerInstallDir";
    static readonly string COMMON_CONFIG_DIRECTORY_KEY = "CommonConfigDirectory";
#if NETSTANDARD
    static readonly string LINUX_CONF_DIRECTORY = "/etc/lpulse";
#endif // NETSTANDARD
    static readonly string PFR_DATA_DIRECTORY_KEY = "PfrDataDirectory";
    static readonly string SQL_REQUESTS_DIRECTORY_KEY = "SqlRequestsDirectory";
    static readonly string LOCAL_CONFIGURATION_DIRECTORY_KEY = "LocalConfigurationDirectory";

    static readonly string DATABASE_NAME_KEY = "DatabaseName";
    static readonly string DATABASE_NAME_DEFAULT = "DatabaseName";

    static readonly string LEM_CTR_NAME_KEY = "LemCtrName";

    #region Members
    string m_defaultLocalConfigurationDirectory = "";

    bool m_valid = false;
    #endregion

    static readonly ILog log = LogManager.GetLogger (typeof (PulseInfo).FullName);

    #region Getters / Setters
    /// <summary>
    /// Installation directory
    /// 
    /// This corresponds to config key "InstallDir"
    /// 
    /// It may not be defined in the future and null returned instead
    /// </summary>
    public static string InstallationDir
    {
      get {
        try {
          var result = Lemoine.Info.ConfigSet.Get<string> (INSTALLATION_DIR_KEY);
          if (log.IsErrorEnabled && string.IsNullOrEmpty (result)) {
            log.Error ("InstallationDir: not defined");
          }
          return result;
        }
        catch (ConfigKeyNotFoundException ex) {
          if (log.IsDebugEnabled) {
            log.Debug ($"InstallationDir: config key {INSTALLATION_DIR_KEY} was not defined => return null", ex);
          }
          return null;
        }
        catch (KeyNotFoundException ex) {
          log.Fatal ($"InstallationDir: (with deprecated KeyNotFoundException) config key {INSTALLATION_DIR_KEY} was not defined => return null", ex);
          return null;
        }
      }
    }

    /// <summary>
    /// Installation directory of the Server
    /// 
    /// This corresponds to config key "PulseServerInstallDir"
    /// 
    /// Return null if it was not defined
    /// </summary>
    public static string PulseServerInstallationDirectory
    {
      get {
        try {
          var result = Lemoine.Info.ConfigSet.Get<string> (PULSE_SERVER_INSTALL_DIR_KEY);
          if (log.IsErrorEnabled && string.IsNullOrEmpty (result)) {
            log.ErrorFormat ("PulseServerInstallationDirectory: not defined");
          }
          return result;
        }
        catch (ConfigKeyNotFoundException ex) {
          log.Error ($"PulseServerInstallationDirectory: config key {PULSE_SERVER_INSTALL_DIR_KEY} was not defined", ex);
          return null;
        }
        catch (KeyNotFoundException ex) {
          log.Fatal ($"PulseServerInstallationDirectory: (with deprecated KeyNotFoundException) config key {PULSE_SERVER_INSTALL_DIR_KEY} was not defined", ex);
          return null;
        }
      }
    }

    /// <summary>
    /// Directory where the common (to all the users) configurations are stored
    /// 
    /// If the directory does not exist, create it
    /// </summary>
    public static string CommonConfigurationDirectory
    {
      get {
        try {
          var commonConfigDirectory = Lemoine.Info.ConfigSet.Get<string> (COMMON_CONFIG_DIRECTORY_KEY);
          if (!string.IsNullOrEmpty (commonConfigDirectory)) {
            if (!Directory.Exists (commonConfigDirectory)) {
              if (log.IsDebugEnabled) {
                log.Debug ($"CommonConfigurationDirectory: create {commonConfigDirectory}");
              }
              Directory.CreateDirectory (commonConfigDirectory);
            }
            return commonConfigDirectory;
          }
          else {
            log.Error ($"CommonConfigurationDirectory: config key {COMMON_CONFIG_DIRECTORY_KEY} returned an empty value {commonConfigDirectory}");
          }
        }
        catch (ConfigKeyNotFoundException ex) {
          log.Error ($"CommonConfigurationDirectory: config key {COMMON_CONFIG_DIRECTORY_KEY} was not defined", ex);
        }
        catch (KeyNotFoundException ex) {
          log.Fatal ($"CommonConfigurationDirectory: (with deprecated KeyNotFoundException) config key {COMMON_CONFIG_DIRECTORY_KEY} was not defined", ex);
        }
#if !NETSTANDARD
        if (true) {
#else // NETSTANDARD
        if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
#endif // NETSTANDARD
          var path = Path.Combine (GetFolderPath (SpecialFolder.CommonApplicationData), "Lemoine", "PULSE");
          log.Warn ($"CommonConfigurationDirectory: consider default windows folder {path}");
          if (!Directory.Exists (path)) {
            if (log.IsDebugEnabled) {
              log.Debug ($"CommonConfigurationDirectory: create {path} (standard Windows path)");
            }
            Directory.CreateDirectory (path);
          }
          return path;
        }
#if NETSTANDARD
        else {
          var path = LINUX_CONF_DIRECTORY;
          log.Warn ($"CommonConfigurationDirectory: consider default linux folder {path}");
          if (!Directory.Exists (path)) {
            if (log.IsDebugEnabled) {
              log.Debug ($"CommonConfigurationDirectory: create {path} (standard Linux path)");
            }
            Directory.CreateDirectory (path);
          }
          return path;
        }
#endif // NETSTANDARD
      }
    }

    /// <summary>
    /// Directory where the local configurations are stored
    /// The configurations are specific to a machine / user
    /// Read and write is allowed
    /// Note: there is no "\" at the end of the path
    /// 
    /// Create it if it does not exist
    /// </summary>
    public static string LocalConfigurationDirectory
    {
      get {
        string defaultLocalConfigurationDirectory = Instance.m_defaultLocalConfigurationDirectory;
        string localConfigurationDirectory = ConfigSet
          .LoadAndGet<string> (LOCAL_CONFIGURATION_DIRECTORY_KEY,
                               defaultLocalConfigurationDirectory);
        if (log.IsDebugEnabled) {
          log.DebugFormat ($"LocalConfigurationDirectory: {localConfigurationDirectory}, default={defaultLocalConfigurationDirectory}");
        }
        if (!Directory.Exists (localConfigurationDirectory)) {
          if (log.IsDebugEnabled) {
            log.Debug ($"LocalConfigurationDirectory: create {localConfigurationDirectory}");
          }
          Directory.CreateDirectory (localConfigurationDirectory);
        }
        return localConfigurationDirectory;
      }
    }

    /// <summary>
    /// Name of the database
    /// </summary>
    public static string DatabaseName
    {
      get {
        return Lemoine.Info.ConfigSet
          .LoadAndGet<string> (DATABASE_NAME_KEY, DATABASE_NAME_DEFAULT);
      }
    }

    /// <summary>
    /// Name of LCTR
    /// </summary>
    public static string LemCtrName
    {
      get {
        var result = Lemoine.Info.ConfigSet.Get<string> (LEM_CTR_NAME_KEY);
        if (log.IsErrorEnabled && string.IsNullOrEmpty (result)) {
          log.ErrorFormat ("LemCtrName: LCTR name not defined");
        }
        return result;
      }
    }

    /// <summary>
    /// Is it valid ?
    /// 
    /// In case it is not, the installation directory and registry version
    /// may be empty
    /// </summary>
    public static bool Valid
    {
      get { return Instance.m_valid; }
    }

    /// <summary>
    /// Use of the Web protocol in the FileRepository client
    /// </summary>
    public static bool UseFileRepositoryWeb
    {
      get {
        return ConfigSet.LoadAndGet<string> (FILE_REPOSITORY_CLIENT_KEY, FILE_REPOSITORY_CLIENT_DEFAULT)
          .Equals (FILE_REPOSITORY_CLIENT_WEB, StringComparison.InvariantCultureIgnoreCase);
      }
    }

    /// <summary>
    /// Use of the Corba protocol in the FileRepository client
    /// </summary>
    public static bool UseFileRepositoryCorba
    {
      get {
        return ConfigSet.LoadAndGet<string> (FILE_REPOSITORY_CLIENT_KEY, FILE_REPOSITORY_CLIENT_DEFAULT)
          .Equals (FILE_REPOSITORY_CLIENT_CORBA, StringComparison.InvariantCultureIgnoreCase);
      }
    }

    /// <summary>
    /// Use of a shared directory in the FileRepository client
    /// </summary>
    public static bool UseFileRepositorySharedDirectory
    {
      get {
        return ConfigSet.LoadAndGet<string> (FILE_REPOSITORY_CLIENT_KEY, FILE_REPOSITORY_CLIENT_DEFAULT)
          .Equals (FILE_REPOSITORY_CLIENT_DIRECTORY, StringComparison.InvariantCultureIgnoreCase);
      }
    }

    /// <summary>
    /// Use multiple FileRepository clients
    /// </summary>
    public static bool UseFileRepositoryMulti
    {
      get {
        return ConfigSet.LoadAndGet<string> (FILE_REPOSITORY_CLIENT_KEY, FILE_REPOSITORY_CLIENT_DEFAULT)
          .Equals (FILE_REPOSITORY_CLIENT_MULTI, StringComparison.InvariantCultureIgnoreCase);
      }
    }

    /// <summary>
    /// Use of a shared directory in the FileRepository client (deprecated)
    /// </summary>
    public static bool UseSharedDirectory
    {
      get {
        return UseFileRepositorySharedDirectory;
      }
    }

    /// <summary>
    /// Shared directory (may not be used => check UseSharedDirectory)
    /// </summary>
    public static string SharedDirectoryPath
    {
      get { return ConfigSet.LoadAndGet<string> (SHARED_DIRECTORY_PATH_KEY, SHARED_DIRECTORY_PATH_DEFAULT); }
    }

    /// <summary>
    /// Path of the pfrdata repository (on the LCTR computer) if it exists, else null
    /// </summary>
    public static string PfrDataDir
    {
      get {
        string path;
        try {
          path = Lemoine.Info.ConfigSet
            .Get<string> (PFR_DATA_DIRECTORY_KEY);
        }
        catch (ConfigKeyNotFoundException ex) {
          log.Debug ($"PfrDataDir: config key {PFR_DATA_DIRECTORY_KEY} was not defined => return null", ex);
          return null;
        }
        catch (KeyNotFoundException ex) {
          log.Fatal ($"PfrDataDir: (with deprecated KeyNotFoundException) config key {PFR_DATA_DIRECTORY_KEY} was not defined => return null", ex);
          return null;
        }

        if (Directory.Exists (path)) {
          if (log.IsDebugEnabled) {
            log.Debug ($"PfrDataDir: return {path}");
          }
          return path;
        }
        else {
          if (log.IsDebugEnabled) {
            log.Debug ($"PfrDataDir: {path} does not exist");
          }
          return null;
        }
      }
    }

    /// <summary>
    /// Path of the sql requests directory (on the LCTR computer) if it exists, else null
    /// </summary>
    public static string SqlRequestsDir
    {
      get {
        string path;
        try {
          path = Lemoine.Info.ConfigSet
            .Get<string> (SQL_REQUESTS_DIRECTORY_KEY);
        }
        catch (ConfigKeyNotFoundException ex) {
          log.Debug ($"SqlRequestsDir: config key {SQL_REQUESTS_DIRECTORY_KEY} was not defined => return null", ex);
          return null;
        }
        catch (KeyNotFoundException ex) {
          log.Fatal ($"SqlRequestsDir: (with deprecated KeyNotFoundException) config key {SQL_REQUESTS_DIRECTORY_KEY} was not defined => return null", ex);
          return null;
        }

        if (Directory.Exists (path)) {
          if (log.IsDebugEnabled) {
            log.Debug ($"SqlRequestsDir: return {path}");
          }
          return path;
        }
        else {
          if (log.IsDebugEnabled) {
            log.Debug ($"SqlRequestsDir: {path} does not exist");
          }
          return null;
        }
      }
    }

    /// <summary>
    /// URL of the web service
    /// </summary>
    public static string WebServiceUrl
    {
      get { return ConfigSet.LoadAndGet<string> (WEB_SERVICE_URL_KEY, WEB_SERVICE_URL_DEFAULT); }
    }

    /// <summary>
    /// URL of the web service
    /// </summary>
    public static string MainWebServiceUrl
    {
      get { return ConfigSet.LoadAndGet<string> (MAIN_WEB_SERVICE_URL_KEY, MAIN_WEB_SERVICE_URL_DEFAULT); }
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor (singleton class !)
    /// </summary>
    private PulseInfo ()
    {
      try {
        Load ();
      }
      catch (Exception ex) {
        log.Error ("PulseInfo: could not get the info in registry", ex);
      }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Reload the Pulse info data
    /// </summary>
    public static void Reload ()
    {
      try {
        Instance.Load ();
      }
      catch (Exception ex) {
        log.Error ("Reload: Load failed", ex);
      }
    }

    void Load ()
    {
      try {
        // User directory (writable and readable)
        var localApplicationData = Environment
          .GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty (localApplicationData)) {
          log.Error ($"Load: LocalApplicationData {localApplicationData} is not defined");
          var home = Environment.GetEnvironmentVariable ("HOME");
          if (string.IsNullOrEmpty (home)) {
            log.Error ($"Load: HOME {home} is not defined");
          }
          else {
            localApplicationData = Path.Combine (home, ".local", "share");
            log.Info ($"Load: fallback localApplicationData to {localApplicationData} from home {home}");
          }
        }
        string pulseDirectory;
#if !NETSTANDARD
        if (true) {
#else // NETSTANDARD
        if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
#endif // NETSTANDARD
          pulseDirectory = "PULSE";
        }
#if NETSTANDARD
        else {
          pulseDirectory = "lpulse";
        }
#endif // NETSTANDARD
        m_defaultLocalConfigurationDirectory = Path.Combine (localApplicationData,
                                                             pulseDirectory);
        try {
          if (!System.IO.Directory.Exists (m_defaultLocalConfigurationDirectory)) {
            System.IO.Directory.CreateDirectory (m_defaultLocalConfigurationDirectory);
          }
        }
        catch (Exception ex1) {
          log.Error ($"Load: creating {m_defaultLocalConfigurationDirectory} failed", ex1);
        }

        this.m_valid = true;
        if (log.IsInfoEnabled) {
          log.Info ($"Load: default local configuration directory={m_defaultLocalConfigurationDirectory}");
        }
      }
      catch (Exception ex) {
        this.m_valid = false;
        log.Error ("Load: exception", ex);
        throw;
      }
    }

    /// <summary>
    /// Create the temp directories if they don't exist
    /// 
    /// This is to fix an issue in case a temp directories does not exist.
    /// </summary>
    public static void CreateTempDirectories ()
    {
      var temp = System.Environment.GetEnvironmentVariable ("TEMP");
      CreateIfNotExists (temp);
      var tmp = System.Environment.GetEnvironmentVariable ("TMP");
      CreateIfNotExists (tmp);
    }

    static void CreateIfNotExists (string directory)
    {
      if (!string.IsNullOrEmpty (directory) && !System.IO.Directory.Exists (directory)) {
        System.IO.Directory.CreateDirectory (directory);
      }
    }
    #endregion // Methods

    #region Instance
    static PulseInfo Instance
    {
      get { return Nested.instance; }
    }

    class Nested
    {
      // Explicit static constructor to tell C# compiler
      // not to mark type as beforefieldinit
      static Nested ()
      {
      }

      internal static readonly PulseInfo instance = new PulseInfo ();
    }
    #endregion
  }
}
