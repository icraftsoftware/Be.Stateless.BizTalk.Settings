#region Copyright & License

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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Be.Stateless.Extensions;
using Be.Stateless.Linq.Extensions;
using Microsoft.EnterpriseSingleSignOn.Interop;

namespace Be.Stateless.BizTalk.Settings.Sso
{
	public sealed class ConfigStore
	{
		#region Nested Type: ConfigStoreProperties

		internal sealed class ConfigStoreProperties : IPropertyBag
		{
			internal ConfigStoreProperties(string affiliateApplicationName, string identifier)
			{
				_affiliateApplicationName = affiliateApplicationName;
				_identifier = identifier;
				Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			}

			#region IPropertyBag Members

			/// <summary>
			/// Reads the value of the specified propName from the Config Store.
			/// </summary>
			/// <param name="propName">Name of the property.</param>
			/// <param name="ptrVar">Value of the property.</param>
			/// <param name="errorLog">Unused.</param>
			void IPropertyBag.Read(string propName, out object ptrVar, int errorLog)
			{
				if (!Properties.TryGetValue(propName, out ptrVar)) throw new KeyNotFoundException($"Property '{propName}' has not been defined in {nameof(ConfigStore)}.");
			}

			/// <summary>
			/// Writes ptrVar as the value of the specified propName to the Config Store.
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
					ssoConfigStore.DeleteConfigInfo(_affiliateApplicationName, _identifier);
				}
				catch (COMException exception)
				{
					if ((uint) exception.ErrorCode != (uint) HResult.ErrorMappingNonExistent) throw;
				}
			}

			/// <summary>
			/// Loads the Config Store from the Enterprise Single Sign-On (SSO).
			/// </summary>
			internal void Load()
			{
				try
				{
					// provision the dictionary with the names of all the properties that are defined at the affiliate application level
					var mapper = new ISSOMapper2();
					mapper.GetFieldInfo(_affiliateApplicationName, out var labels, out _);
					// skip contact, which is a dummy 1st field
					labels.Where(l => l != AffiliateApplication.DEFAULT_CONTACT_INFO).ForEach(l => Properties.Add(l, default));
					// populate dictionary with all the property values that have been set
					var configStore = new ISSOConfigStore();
					configStore.GetConfigInfo(_affiliateApplicationName, _identifier, SSOFlag.SSO_FLAG_RUNTIME, this);
				}
				catch (COMException exception)
				{
					if ((uint) exception.ErrorCode != (uint) HResult.ErrorMappingNonExistent) throw;
				}
			}

			/// <summary>
			/// Reloads the Config Store with fresh values from the Enterprise Single Sign-On (SSO).
			/// </summary>
			internal void Reload()
			{
				try
				{
					// reload does not need to provision the dictionary with the names of all the properties but only to populate it with fresh values
					var configStore = new ISSOConfigStore();
					configStore.GetConfigInfo(_affiliateApplicationName, _identifier, SSOFlag.SSO_FLAG_RUNTIME, this);
				}
				catch (COMException exception)
				{
					if ((uint) exception.ErrorCode != (uint) HResult.ErrorMappingNonExistent) throw;
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
						configStore.SetConfigInfo(_affiliateApplicationName, _identifier, this);
						break;
					}
					catch (COMException exception)
					{
						// see https://github.com/BTDF/DeploymentFramework/blob/4f047b6ac7067d369365c8776aefe3f4958278a7/src/Tools/SSOSettingsFileImport/SSOSettingsFileImport/SSOHelper.cs#L75
						// This error occurs randomly and in virtually all cases, an immediate retry succeeds.
						if ((uint) exception.ErrorCode != (uint) HResult.ErrorSsoDbExternalCredentialsAreMoreRecent) throw;
						if (++retryCount >= 5) throw;
					}
				}
			}

			private readonly string _affiliateApplicationName;
			private readonly string _identifier;
		}

		#endregion

		internal ConfigStore(string affiliateApplicationName, string configStoreIdentifier)
		{
			_affiliateApplicationName = affiliateApplicationName ?? throw new ArgumentNullException(nameof(affiliateApplicationName));
			Identifier = configStoreIdentifier ?? throw new ArgumentNullException(nameof(configStoreIdentifier));
		}

		/// <summary>
		/// Elapsed time since the application settings were last refreshed or, more generally, synchronized with the Enterprise
		/// Single Sign-On (SSO) Config Store.
		/// </summary>
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
		public TimeSpan Age => DateTimeOffset.UtcNow.Subtract(_timestamp);

		public string Identifier { get; }

		public IDictionary<string, object> Properties => IsDefault ? Settings : StoreProperties.Properties;

		private bool IsDefault => Identifier == ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER;

		private IDictionary<string, object> Settings => LazyInitializer.EnsureInitialized(ref _settings, ref _settingsLoaded, ref _lock, DeserializeSettings);

		private ConfigStoreProperties StoreProperties => LazyInitializer.EnsureInitialized(
			ref _storeProperties,
			ref _propertiesLoaded,
			ref _lock,
			() => {
				var configStoreProperties = new ConfigStoreProperties(_affiliateApplicationName, Identifier);
				configStoreProperties.Load();
				_timestamp = DateTimeOffset.UtcNow;
				return configStoreProperties;
			});

		public ConfigStore AgedLessThan(TimeSpan elapsedTime)
		{
			if (_propertiesLoaded && Age > elapsedTime) Reload();
			return this;
		}

		/// <summary>
		/// Deletes the application settings, i.e the Enterprise Single Sign-On (SSO) Config Store.
		/// </summary>
		public void Delete()
		{
			if (!IsDefault) throw new InvalidOperationException($"Cannot delete a {nameof(ConfigStore)} other than the default one.");
			StoreProperties.Delete();
			_timestamp = default;
		}

		/// <summary>
		/// Reloads the Config Store with fresh values from the Enterprise Single Sign-On (SSO).
		/// </summary>
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
		public void Reload()
		{
			lock (_lock)
			{
				_settingsLoaded = false;
				_propertiesLoaded = false;
			}
			StoreProperties.Reload();
		}

		/// <summary>
		/// Saves the application settings, i.e the Enterprise Single Sign-On (SSO) Config Store.
		/// </summary>
		/// <remarks>
		/// Save does not need to be thread-safe because only a process run by a BizTalk Administrator will be allowed to call
		/// the <see cref="Save"/> method, and this will/should occur at deployment time in a single process.
		/// </remarks>
		public void Save()
		{
			if (!IsDefault) throw new InvalidOperationException($"Cannot save or overwrite the properties of a {nameof(ConfigStore)} other than the default one.");

			var store = StoreProperties;
			store.Properties[AffiliateApplication.DEFAULT_SETTINGS_KEY] = SerializeSettings();
			store.Save();
			_timestamp = DateTimeOffset.UtcNow;
		}

		private string SerializeSettings()
		{
			return JsonSerializer.Serialize(Settings);
		}

		private Dictionary<string, object> DeserializeSettings()
		{
			// serialized data is oblivious about OrdinalIgnoreCase and forces us to manually take care of the deserialization
			var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			if (StoreProperties.Properties.TryGetValue(AffiliateApplication.DEFAULT_SETTINGS_KEY, out var settingsValue))
			{
				(settingsValue as string).IfNotNullOrWhiteSpace(
					sv => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sv)
						// TODO should use JsonElement.ValueKind to add a typed object and not always a string
						.ForEach(kvp => result.Add(kvp.Key, kvp.Value.ToString())));
			}
			return result;
		}

		private static object _lock = new object();
		private readonly string _affiliateApplicationName;
		private bool _propertiesLoaded;
		private IDictionary<string, object> _settings;
		private bool _settingsLoaded;
		private ConfigStoreProperties _storeProperties;
		private DateTimeOffset _timestamp;
	}
}
