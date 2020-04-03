﻿#region Copyright & License

// Copyright © 2012 - 2020 François Chabot
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Be.Stateless.Linq.Extensions;
using Microsoft.EnterpriseSingleSignOn.Interop;

namespace Be.Stateless.BizTalk.Settings.Sso
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
	public sealed class ConfigStore
	{
		#region Nested Type: ConfigStoreProperties

		internal sealed class ConfigStoreProperties : IPropertyBag
		{
			internal ConfigStoreProperties(string name, string identifier)
			{
				_name = name;
				_identifier = identifier;
				Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			}

			#region IPropertyBag Members

			/// <summary>
			/// Reads the valuer for the specified propName of the property.
			/// </summary>
			/// <param name="propName">Name of the property.</param>
			/// <param name="ptrVar">Value of the property.</param>
			/// <param name="errorLog">Unused.</param>
			void IPropertyBag.Read(string propName, out object ptrVar, int errorLog)
			{
				if (!Properties.TryGetValue(propName, out ptrVar)) throw new KeyNotFoundException($"Property '{propName}' has not been defined in {nameof(ConfigStore)}.");
			}

			/// <summary>
			/// Writes the ptrVar for the specified propName of the property.
			/// </summary>
			/// <param name="propName">Name of the property.</param>
			/// <param name="ptrVar">Value of the property.</param>
			void IPropertyBag.Write(string propName, ref object ptrVar)
			{
				if (!Properties.ContainsKey(propName)) throw new KeyNotFoundException($"Property '{propName}' has not been defined in {nameof(ConfigStore)}.");
				Properties[propName] = ptrVar;
			}

			#endregion

			internal IDictionary<string, object> Properties { get; }

			/// <summary>
			/// Deletes the Config Store from the Enterprise Single Sign-On (SSO).
			/// </summary>
			internal void Delete()
			{
				try
				{
					var ssoConfigStore = new ISSOConfigStore();
					ssoConfigStore.DeleteConfigInfo(_name, _identifier);
				}
				catch (COMException exception)
				{
					// Error Code = 'The mapping does not exist. For Config Store applications, the config info has not been set.'
					if ((uint) exception.ErrorCode != 0xC0002A05) throw;
				}
			}

			/// <summary>
			/// Loads the Config Store from the Enterprise Single Sign-On (SSO).
			/// </summary>
			internal void Load()
			{
				// TODO reload the ConfigStore if older than 60 seconds but ?? what if property dictionary is dirty ??
				try
				{
					// populate dictionary with all properties
					var mapper = new ISSOMapper2();
					mapper.GetFieldInfo(_name, out var labels, out _);
					// skip contact, which is the 1st dummy field
					labels.Where(l => l != AffiliateApplication.DEFAULT_CONTACT_INFO).ForEach(l => Properties.Add(l, default));
					// populate dictionary with all values
					var configStore = new ISSOConfigStore();
					configStore.GetConfigInfo(_name, _identifier, SSOFlag.SSO_FLAG_RUNTIME, this);
				}
				catch (COMException exception)
				{
					// Error Code = 'The mapping does not exist. For Config Store applications, the config info has not been set.'
					if ((uint) exception.ErrorCode != 0xC0002A05) throw;
				}
			}

			/// <summary>
			/// Saves the Config Store in the Enterprise Single Sign-On (SSO).
			/// </summary>
			internal void Save()
			{
				var configStore = new ISSOConfigStore();
				var retryCount = 0;
				while (true)
				{
					try
					{
						configStore.SetConfigInfo(_name, _identifier, this);
						break;
					}
					catch (COMException exception)
					{
						// see https://github.com/BTDF/DeploymentFramework/blob/4f047b6ac7067d369365c8776aefe3f4958278a7/src/Tools/SSOSettingsFileImport/SSOSettingsFileImport/SSOHelper.cs#L75
						// This error occurs randomly and in virtually all cases, an immediate retry succeeds.
						// Error Code = 'The external credentials in the SSO database are more recent.'
						if ((uint) exception.ErrorCode != 0xC0002A40) throw;
						if (++retryCount >= 5) throw;
					}
				}
			}

			private readonly string _identifier;
			private readonly string _name;
		}

		#endregion

		internal ConfigStore(string affiliateApplicationName, string configStoreIdentifier)
		{
			_affiliateApplicationName = affiliateApplicationName ?? throw new ArgumentNullException(nameof(affiliateApplicationName));
			Identifier = configStoreIdentifier ?? throw new ArgumentNullException(nameof(configStoreIdentifier));
			_lazyConfigStoreProperties = new Lazy<ConfigStoreProperties>(
				() => {
					var configStoreProperties = new ConfigStoreProperties(affiliateApplicationName, configStoreIdentifier);
					configStoreProperties.Load();
					return configStoreProperties;
				});
		}

		public string Identifier { get; }

		public IDictionary<string, object> Properties => _lazyConfigStoreProperties.Value.Properties;

		private bool IsDefault => Identifier == ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER;

		/// <summary>
		/// Deletes the application settings, i.e the Enterprise Single Sign-On (SSO) Config Store.
		/// </summary>
		public void Delete()
		{
			if (!IsDefault) throw new InvalidOperationException($"Cannot delete a {nameof(ConfigStore)} other than the default one.");
			_lazyConfigStoreProperties.Value.Delete();
		}

		/// <summary>
		/// Saves the application settings, i.e the Enterprise Single Sign-On (SSO) Config Store.
		/// </summary>
		public void Save()
		{
			if (!IsDefault) throw new InvalidOperationException($"Cannot save or overwrite the properties of a {nameof(ConfigStore)} other than the default one.");
			var ssoAdmin = new ISSOAdmin();
			_lazyConfigStoreProperties.Value.Properties.Keys.ForEach(
				key => {
					try
					{
						ssoAdmin.CreateFieldInfo(_affiliateApplicationName, key, SSOFlag.SSO_FLAG_NONE);
					}
					catch (COMException exception)
					{
						// Error Code = 'The field already exists.'
						if ((uint) exception.ErrorCode != 0xC0002A06) throw;
					}
				});
			ssoAdmin.UpdateApplication(_affiliateApplicationName, null, null, null, null, SSOFlag.SSO_FLAG_ENABLED, SSOFlag.SSO_FLAG_ENABLED);
			_lazyConfigStoreProperties.Value.Save();
		}

		private readonly string _affiliateApplicationName;
		private readonly Lazy<ConfigStoreProperties> _lazyConfigStoreProperties;
	}
}