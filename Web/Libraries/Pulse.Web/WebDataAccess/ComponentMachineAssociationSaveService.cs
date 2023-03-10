// Copyright (C) 2009-2023 Lemoine Automation Technologies
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Lemoine.Model;
using Lemoine.ModelDAO;

using Lemoine.Core.Log;
using Pulse.Web.CommonResponseDTO;
using Pulse.Web.WebDataAccess.CommonResponseDTO;
using Lemoine.Extensions.Web.Responses;
using Lemoine.Web;

namespace Pulse.Web.WebDataAccess
{
  /// <summary>
  /// ComponentMachineAssociationSave Service.
  /// </summary>
  public class ComponentMachineAssociationSaveService: GenericSaveService<Pulse.Web.WebDataAccess.ComponentMachineAssociationSave>
  {
    static readonly ILog log = LogManager.GetLogger (typeof (ComponentMachineAssociationSaveService).FullName);

    #region Constructors
    /// <summary>
    /// 
    /// </summary>
    public ComponentMachineAssociationSaveService ()
    {
    }
    #endregion // Constructors

    #region Methods
    /// <summary>
    /// Response to GET request (no cache)
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public override object GetSync (Pulse.Web.WebDataAccess.ComponentMachineAssociationSave request)
    {
      IComponentMachineAssociation componentMachineAssociation;

      using (IDAOSession session = ModelDAOHelper.DAOFactory.OpenSession ()) {
        using (IDAOTransaction transaction = session.BeginTransaction ("ComponentMachineAssociationSaveService")) {
          IMachine machine = ModelDAOHelper.DAOFactory.MachineDAO
            .FindById (request.MachineId);
          if (null == machine) {
            log.ErrorFormat ("Get: " +
                             "Machine with {0} does not exist",
                             request.MachineId);
            transaction.Commit ();
            return new ErrorDTO ("No machine with id " + request.MachineId,
                                 ErrorStatus.WrongRequestParameter);
          }

          UtcDateTimeRange range = new UtcDateTimeRange (request.Range);

          if (request.ComponentId.HasValue) {
            IComponent component = ModelDAOHelper.DAOFactory.ComponentDAO
              .FindById (request.ComponentId.Value);
            if (null == component) {
              log.ErrorFormat ("Get: " +
                               "No reason with ID {0}",
                               request.ComponentId.Value);
              transaction.Commit ();
              return new ErrorDTO ("No work order with id " + request.ComponentId.Value,
                                   ErrorStatus.WrongRequestParameter);
            }
            else {
              componentMachineAssociation = ModelDAOHelper.ModelFactory
                .CreateComponentMachineAssociation (machine, component, range);
            }
          }
          else {
            componentMachineAssociation = ModelDAOHelper.ModelFactory
              .CreateComponentMachineAssociation (machine, null, range);
          }
          if (request.RevisionId.HasValue) {
            if (-1 == request.RevisionId.Value) { // auto-revision
              IRevision revision = ModelDAOHelper.ModelFactory.CreateRevision ();
              revision.Application = "Lem_AspService";
              revision.IPAddress = GetRequestRemoteIp ();
              ModelDAOHelper.DAOFactory.RevisionDAO.MakePersistent (revision);
              componentMachineAssociation.Revision = revision;
            }
            else {
              IRevision revision = ModelDAOHelper.DAOFactory.RevisionDAO
                .FindById (request.RevisionId.Value);
              if (null == revision) {
                log.WarnFormat ("Get: " +
                                "No revision with ID {0}",
                                request.RevisionId.Value);
              }
              else {
                componentMachineAssociation.Revision = revision;
              }
            }
          }

          ModelDAOHelper.DAOFactory.ComponentMachineAssociationDAO
            .MakePersistent (componentMachineAssociation);

          transaction.Commit ();
        }
      }

      Debug.Assert (null != componentMachineAssociation);
      return new SaveModificationResponseDTO (componentMachineAssociation);
    }
    #endregion // Methods
  }
}
