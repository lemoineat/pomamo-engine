// Copyright (C) 2009-2023 Lemoine Automation Technologies
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Lemoine.Core.Cache;
using Lemoine.Core.ExceptionManagement;

using Lemoine.Core.Log;
using Lemoine.Extensions.Web.Attributes;
using Lemoine.Extensions.Web.Interfaces;
using Lemoine.Extensions.Web.Responses;

namespace Lemoine.Extensions.Web.Services
{
  /// <summary>
  /// Base save service (not cached)
  /// </summary>
  public abstract class GenericSyncSaveService<InputDTO>
    : IHandler, IRemoteIpSupport, ISyncSaveService<InputDTO>
  {
    readonly ILog log = LogManager.GetLogger (typeof (GenericSyncSaveService<InputDTO>).FullName);

    #region Getters / Setters
    /// <summary>
    /// Cache client
    /// 
    /// Be careful ! This is not always set. It can be null
    /// </summary>
    public ICacheClient CacheClient
    {
      get; private set;
    }
    
    /// <summary>
    /// Remote Ip
    /// </summary>
    public string RemoteIp { get; set; }
    #endregion // Getters / Setters

    #region Constructors
    /// <summary>
    /// Constructor
    /// </summary>
    protected GenericSyncSaveService ()
    {
      this.CacheClient = null;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    protected GenericSyncSaveService (ICacheClient cacheClient)
    {
      this.CacheClient = cacheClient;
    }
    #endregion // Constructors

    #region Methods
    /// <summary>
    /// Get
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public abstract object GetSync (InputDTO request);

    /// <summary>
    /// Get implementation
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public Task<object> Get (InputDTO request)
    {
      var result = GetSync (request);
      return Task.FromResult (result);
    }

    /// <summary>
    /// Fetch IP of request
    /// </summary>
    /// <returns></returns>
    public string GetRequestRemoteIp ()
    {
      return this.RemoteIp;
    }

    /// <summary>
    /// <see cref="IRemoteIpSupport"/>
    /// </summary>
    /// <param name="remoteIp"></param>
    /// <returns></returns>
    public void SetRemoteIp (string remoteIp)
    {
      this.RemoteIp = remoteIp;
    }
    #endregion // Methods
  }
}
