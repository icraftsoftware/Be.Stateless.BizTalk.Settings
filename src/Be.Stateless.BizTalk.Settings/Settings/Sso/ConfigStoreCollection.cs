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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EnterpriseSingleSignOn.Interop;

namespace Be.Stateless.BizTalk.Settings.Sso
{
	/// <summary>
	/// Dictionary of <see cref="ConfigStore"/>s associated to a given <see cref="AffiliateApplication"/>.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
	public sealed class ConfigStoreCollection : IReadOnlyDictionary<string, ConfigStore>
	{
		internal ConfigStoreCollection(AffiliateApplication affiliateApplication)
		{
			if (affiliateApplication == null) throw new ArgumentNullException(nameof(affiliateApplication));
			var mapper = new ISSOMapper2();
			var applicationMappings = mapper.GetMappingsForExternalUser(affiliateApplication.Name, null);
			var configStoreIdentifiers = applicationMappings.Cast<ISSOMapping>()
				.Where(m => m.WindowsDomainName == "$ConfigStore$")
				.Select(m => m.WindowsUserName)
				// ensure the default ConfigStore identifier is in the list of configStore's id to instantiate
				.Union(Enumerable.Repeat(DEFAULT_CONFIG_STORE_IDENTIFIER, affiliateApplication.HasOwnership ? 1 : 0));
			_configStoreDictionary = configStoreIdentifiers
				.Select(id => new ConfigStore(affiliateApplication.Name, id))
				.ToDictionary(cs => cs.Identifier);
		}

		#region IReadOnlyDictionary<string,ConfigStore> Members

		public IEnumerator<KeyValuePair<string, ConfigStore>> GetEnumerator()
		{
			return _configStoreDictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable) _configStoreDictionary).GetEnumerator();
		}

		public int Count => _configStoreDictionary.Count;

		public bool ContainsKey(string key)
		{
			return _configStoreDictionary.ContainsKey(key);
		}

		public bool TryGetValue(string key, out ConfigStore value)
		{
			return _configStoreDictionary.TryGetValue(key, out value);
		}

		public ConfigStore this[string key] => _configStoreDictionary[key];

		public IEnumerable<string> Keys => _configStoreDictionary.Keys;

		public IEnumerable<ConfigStore> Values => _configStoreDictionary.Values;

		#endregion

		/// <summary>
		/// The <see cref="ConfigStore"/> that has been created by BizTalk.Factory, which is the only one that this library will
		/// allow to edit.
		/// </summary>
		public ConfigStore Default => _configStoreDictionary.TryGetValue(DEFAULT_CONFIG_STORE_IDENTIFIER, out var store) ? store : null;

		internal bool ContainsForeignConfigStores => Keys.Any(key => key != DEFAULT_CONFIG_STORE_IDENTIFIER);

		internal const string DEFAULT_CONFIG_STORE_IDENTIFIER = "{86ca4d07-f4da-4386-9ed4-dab5b83b9e8b}";
		private readonly IReadOnlyDictionary<string, ConfigStore> _configStoreDictionary;
	}
}
