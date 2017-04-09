/// <summary>
/// 
/// </summary>
namespace EIV.OData.Core
{
    // Remove dependency on the Proxy Class
    //using com.cairone.odataexample;
    using ITrackable;
    using Microsoft.OData.Client;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Security.Principal;

    /// <summary>
    /// 
    /// </summary>
    public sealed class ODataServiceContext   // <T> where T : Microsoft.OData.Client.DataServiceContext
    {
        private static volatile ODataServiceContext _instance = null;
        private static readonly object InstanceLoker = new object();

        // Dependency on EIV.Data.Proxy
        // Trying to remove this dependency
        //private ODataExample container = null;
        private DataServiceContext container = null;

        private ODataErrors errors = null;

        private bool isConnected = false;

        private string userName = string.Empty;
        private string password = string.Empty;
        private string domainName = string.Empty;

        private ODataServiceContext()
        {
            this.errors = new ODataErrors();
        }

        public static ODataServiceContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLoker)
                    {
                        if (_instance == null)
                            _instance = new ODataServiceContext();
                    }
                }

                return _instance;
            }
        }

        public string UserName
        {
            get
            {
                return this.userName;
            }
            set
            {
                this.userName = value;
            }
        }

        public string Password
        {
            internal get
            {
                return this.password;
            }
            set
            {
                this.password = value;
            }
        }

        public string DomainName
        {
            get
            {
                return this.domainName;
            }
            set
            {
                this.domainName = value;
            }
        }

        public ODataErrors Errors
        {
            get
            {
                return this.errors;
            }
        }

        // where X : DataServiceContext
        public bool Connect<X>(string serviceUri) where X : DataServiceContext
        {
            Uri thisUri;

            if (string.IsNullOrEmpty(serviceUri))
            {
                return false;
            }

            bool rst = Uri.TryCreate(serviceUri, UriKind.Absolute, out thisUri);
            if (!rst)
            {
                return false;
            }
            if (this.isConnected)
            {
                return true;
            }

            Type contextType = typeof(X);

            // Paranoic!
            if (!this.ValidateObjectType(contextType))
            {
                return false;
            }

            try
            {
                // http://odata.github.io/odata.net/04-06-use-client-hooks-in-odata-client/
                this.container = (X)Activator.CreateInstance(contextType, thisUri);  //  DataServiceContext(thisUri);                  // ODataExample(thisUri);

                this.container.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

                this.isConnected = true;

                return true;
            }
            catch (Exception ex)
            {
                this.errors.Add(ex);
            }

            return false;
        }

        public void Disconnect()
        {
            if (!this.isConnected)
            {
                return;
            }

            this.container = null;

            this.isConnected = false;
        }

        // This is dependent on the Proxy class
        //
        // Apparently, 'T' and 'entityName' param can be of different names (e.g. Pais and Paises)
        public ObservableCollection<T> GetEntities<T>(string entityName, IDictionary<string, object> filters) where T : class
        {
            IEnumerable<T> result = null;

            if (string.IsNullOrEmpty(entityName))
            {
                return null;
            }

            if (!this.isConnected)
            {
                return null;
            }
            try
            {
                //  Paises.OrderBy(x => x.nombre).ToList();
                //var list = this.container.CreateQuery<T>(entityName).ToList();
                DataServiceQuery<T> query = this.container.CreateQuery<T>(entityName);

                // Country?$filter=Name eq 'USA'
                
                if (filters == null)
                {
                    result = query.Execute();
                }
                else {
                    // Test this ...
                    foreach (string filter in filters.Keys)
                    {
                        object filterValue = filters[filter];
                        if (filterValue != null)
                        {
                            // Immutability here ?
                            query = query.AddQueryOption(filter, filterValue);
                        }
                    }

                    result = query.Execute();
                }

                // Synchronous operation ???
                var list = result.ToList();

                return new ObservableCollection<T>(list);
            }
            catch (Exception ex)  // will this work for all types of errors?
            {
                this.errors.Add(ex);
            }

            return null;
        }
        public void ProcessOperations(TrackableEntities list)
        {
            if (!this.isConnected)
            {
                return;
            }

            if (list == null)
            {
                return;
            }

            // We could have entities of different type within the same collection, couldn't we?
            /*
            if (string.IsNullOrEmpty(list.Name))
            {
                return;
            }*/

            foreach (TrackableEntity item in list.Items)
            {
                object entity = item.Entity;

                switch (item.OperationType)
                {
                    case TrackableEntity.Operation.Insert:
                        
                        // I need to make it generic!
                        Type entityType = entity.GetType();
                        string entityName = entityType.Name;

                        this.container.AddObject(entityName, entity);
                        break;

                    case TrackableEntity.Operation.Update:
                        this.container.UpdateObject(entity);
                        break;

                    case TrackableEntity.Operation.Delete:
                        this.container.DeleteObject(entity);
                        break;
                }
            }
        }
        // Needs refactoring
        public bool SubmitChanges(bool batchMode = true)
        {
            DataServiceResponse response = null;

            if (!this.isConnected)
            {
                return false;
            }

            this.container.Format.UseJson();

            // Throws a funny exception! (only if Java Service)
            // SaveChangesOptions.BatchWithSingleChangeset

            try
            {
                if (batchMode)
                {
                    // Synchronous operation ???
                    response = this.container.SaveChanges(SaveChangesOptions.ReplaceOnUpdate | SaveChangesOptions.BatchWithSingleChangeset);

                    // Makes no sense if not 'SaveChangesOptions.BatchWithSingleChangeset'
                    if (!response.IsBatchResponse)
                    {
                        // Some error here
                        response = null;

                        return false;
                    }
                }
                else
                {
                    // Synchronous operation ???
                    response = this.container.SaveChanges(SaveChangesOptions.ReplaceOnUpdate);
                }

                // fix later
                this.errors.Add(response);

                return true;
            }
            catch (Exception ex)  // will this work for all types of errors?
            {
                this.errors.Add(ex);
            }

            return false;
        }

        // BaseType = {Name = "DataServiceContext" FullName = "Microsoft.OData.Client.DataServiceContext"}
        private bool ValidateObjectType(Type objectType)
        {
            if (objectType == null)
            {
                return false;
            }

            if (objectType.BaseType != null)
            {
                string baseTypeName = objectType.BaseType.Name;
                string baseTypeFullName = objectType.BaseType.FullName;

                if (string.IsNullOrEmpty(baseTypeName) || string.IsNullOrEmpty(baseTypeFullName))
                {
                    return false;
                }

                // This should be a constant
                if (!baseTypeName.Equals("DataServiceContext"))
                {
                    return false;
                }

                if (!baseTypeFullName.Equals("Microsoft.OData.Client.DataServiceContext"))
                {
                    return false;
                }
            }

            return true;
        }

        // Change to private later on
        private void GenerateNetworkCredentials()
        {
            if (string.IsNullOrEmpty(this.userName) || string.IsNullOrEmpty(this.password))
            {
                return;
            }
            WindowsIdentity wi = System.Security.Principal.WindowsIdentity.GetCurrent();

            NetworkCredential nc = new NetworkCredential(this.userName, this.password, this.domainName);
        }
    }
}