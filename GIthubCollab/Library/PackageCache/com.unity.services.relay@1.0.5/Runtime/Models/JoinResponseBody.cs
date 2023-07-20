//-----------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by the C# SDK Code Generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//-----------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Relay.Http;



namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// JoinResponseBody model
    /// </summary>
    [Preserve]
    [DataContract(Name = "JoinResponseBody")]
    public class JoinResponseBody
    {
        /// <summary>
        /// Creates an instance of JoinResponseBody.
        /// </summary>
        /// <param name="meta">meta param</param>
        /// <param name="data">data param</param>
        [Preserve]
        public JoinResponseBody(ResponseMeta meta, JoinData data)
        {
            Meta = meta;
            Data = data;
        }

        /// <summary>
        /// Parameter meta of JoinResponseBody
        /// </summary>
        [Preserve]
        [DataMember(Name = "meta", IsRequired = true, EmitDefaultValue = true)]
        public ResponseMeta Meta{ get; }
        
        /// <summary>
        /// Parameter data of JoinResponseBody
        /// </summary>
        [Preserve]
        [DataMember(Name = "data", IsRequired = true, EmitDefaultValue = true)]
        public JoinData Data{ get; }
    
        /// <summary>
        /// Formats a JoinResponseBody into a string of key-value pairs for use as a path parameter.
        /// </summary>
        /// <returns>Returns a string representation of the key-value pairs.</returns>
        internal string SerializeAsPathParam()
        {
            var serializedModel = "";

            if (Meta != null)
            {
                serializedModel += "meta," + Meta.ToString() + ",";
            }
            if (Data != null)
            {
                serializedModel += "data," + Data.ToString();
            }
            return serializedModel;
        }

        /// <summary>
        /// Returns a JoinResponseBody as a dictionary of key-value pairs for use as a query parameter.
        /// </summary>
        /// <returns>Returns a dictionary of string key-value pairs.</returns>
        internal Dictionary<string, string> GetAsQueryParam()
        {
            var dictionary = new Dictionary<string, string>();

            return dictionary;
        }
    }
}
