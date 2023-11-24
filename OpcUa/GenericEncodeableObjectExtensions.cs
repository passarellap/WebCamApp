//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 912 $
// $LastChangedDate: 2018-04-16 15:39:03 +0200 (Mo., 16 Apr 2018) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnifiedAutomation.UaBase;
using TypeInfo = UnifiedAutomation.UaBase.TypeInfo;
#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public static class GenericEncodeableObjectExtensions
	{
		#region Public Methods
		public static void ReadFromRealObject(this GenericEncodeableObject genericObj, object realObj, GenericStructureDataType datatype)
		{
			if (realObj is null) return;

			IDictionary<string, PropertyInfo> properties = null;

			var namedValues = realObj as IDictionary<string, object>;
			if (namedValues == null)
			{
				properties = realObj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
					.ToDictionary(pi => pi.Name);
			}

			for (var i = 0; i < genericObj.TypeDefinition.Count; ++i)
			{
				try
				{
					var valueDataType = GetType(genericObj[i].DataType);

					var value = GetPropertyValue(genericObj.TypeDefinition[i].Name, namedValues, realObj, properties);
					if (value != null)
					{
						//Gestione Guid
						if (value.GetType() == typeof(Uuid) && valueDataType == typeof(string))
							value = value.ToString();

						try
						{
							value = Convert.ChangeType(value, valueDataType);
						}
						catch(Exception eeee)
						{
							//Impossibile castare il valore.. capita coi GUID inizializzati a 0000000...
						}
					}

					genericObj[i] = new Variant(value, TypeInfo.Construct(valueDataType));
				}
				catch(ArgumentOutOfRangeException e)
				{					

					GenericStructureDataType innerType = (GenericStructureDataType)datatype[i].TypeDescription;
					var innerObject = new GenericEncodeableObject(innerType);
					object innerRealObject;
					try
					{
						innerRealObject = properties[genericObj.TypeDefinition[i].Name].GetValue(realObj);
					}
					catch(Exception ee)
					{
						innerRealObject = null;
					}

					ReadFromRealObject(innerObject, innerRealObject, innerType);
					genericObj[i] = innerObject;
					
				}				
			}
		}

		public static void WriteToRealObject(this GenericEncodeableObject genericObj, object realObj)
		{
			IDictionary<string, PropertyInfo> properties = null;

			var namedValues = realObj as IDictionary<string, object>;
			if (namedValues == null)
			{
				properties = realObj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
					.ToDictionary(pi => pi.Name);
			}

			for (var i = 0; i < genericObj.TypeDefinition.Count; ++i)
			{
				object value;

				if (genericObj[i].DataType == BuiltInType.Guid)
				{
					value = (Guid)genericObj[i].ToGuid();
				}
				else
				{
					value = genericObj[i].Value;
				}

				SetPropertyValue(value, genericObj.TypeDefinition[i].Name, namedValues, realObj, properties);
			}
		}
		#endregion

		#region Private Methods
		private static object GetPropertyValue(string propertyName, IDictionary<string, object> namedValues, object composite, IDictionary<string, PropertyInfo> properties)
		{
			object value;

			if (namedValues != null)
			{
				namedValues.TryGetValue(propertyName, out value);
			}
			else
			{
				PropertyInfo property;
				value = properties.TryGetValue(propertyName, out property) ? property.GetValue(composite) : null;
			}

			return value;
		}

		private static void SetPropertyValue(object value, string propertyName, IDictionary<string, object> namedValues, object composite, IDictionary<string, PropertyInfo> properties)
		{
			if (namedValues != null)
			{
				namedValues[propertyName] = value;
			}
			else
			{
				PropertyInfo property;
				if (properties.TryGetValue(propertyName, out property))
				{
					value = Convert.ChangeType(value, property.PropertyType);
					property.SetValue(composite, value);
				}
			}
		}

		private static Type GetType(BuiltInType builtInType)
		{
			switch (builtInType)
			{
				case BuiltInType.Boolean:
					return typeof(bool);
				case BuiltInType.SByte:
					return typeof(sbyte);
				case BuiltInType.Byte:
					return typeof(byte);
				case BuiltInType.Int16:
					return typeof(short);
				case BuiltInType.UInt16:
					return typeof(ushort);
				case BuiltInType.Int32:
					return typeof(int);
				case BuiltInType.UInt32:
					return typeof(uint);
				case BuiltInType.Int64:
					return typeof(long);
				case BuiltInType.UInt64:
					return typeof(ulong);
				case BuiltInType.Float:
					return typeof(float);
				case BuiltInType.Double:
					return typeof(double);
				case BuiltInType.String:
					return typeof(string);
				case BuiltInType.DateTime:
					return typeof(DateTime);
				case BuiltInType.Guid:
					return typeof(Guid);
				case BuiltInType.Enumeration:
					return typeof(int);
					/*
				case BuiltInType.Null:
					break;
				case BuiltInType.ByteString:
					break;
				case BuiltInType.XmlElement:
					break;
				case BuiltInType.NodeId:
					break;
				case BuiltInType.ExpandedNodeId:
					break;
				case BuiltInType.StatusCode:
					break;
				case BuiltInType.QualifiedName:
					break;
				case BuiltInType.LocalizedText:
					break;
				case BuiltInType.ExtensionObject:
					break;
				case BuiltInType.DataValue:
					break;
				case BuiltInType.Variant:
					break;
				case BuiltInType.DiagnosticInfo:
					break;
				case BuiltInType.Number:
					break;
				case BuiltInType.Integer:
					break;
				case BuiltInType.UInteger:
					break;
					*/
				default:
					throw new ArgumentOutOfRangeException(nameof(builtInType), builtInType, null);
			}
		}
		#endregion
	}
}