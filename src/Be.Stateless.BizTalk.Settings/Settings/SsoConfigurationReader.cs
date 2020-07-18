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
using System.Diagnostics.CodeAnalysis;
using Be.Stateless.BizTalk.Runtime.Caching;
using Be.Stateless.BizTalk.Settings.Sso;

namespace Be.Stateless.BizTalk.Settings
{
	/// <summary>
	/// <see cref="ISsoConfigurationReader"/> implementation that guarantees fresh readings, or not older than 60 seconds, from
	/// the default <see cref="ConfigStore"/> of a given <see cref="AffiliateApplication"/>.
	/// </summary>
	/// <seealso cref="AffiliateApplication.ConfigStores"/>
	/// <seealso cref="ConfigStoreCollection.Default">ConfigStores.Default</seealso>
	public class SsoConfigurationReader : ISsoConfigurationReader
	{
		/// <summary>
		/// <see cref="SsoConfigurationReader"/> singleton instance.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Provides an internal property setter for the sake of mocking and unit testing.
		/// </para>
		/// <para>
		/// Notice that to circumvent the internal visibility of the <see cref="Instance"/> property setter for clients outside
		/// of the realm of BizTalk.Factory, the <c>Be.Stateless.BizTalk.Unit</c> assembly, which is not GAC deployed, provides a
		/// public setter for the <see cref="Instance"/> property that allows clients of this library to safely inject an <see
		/// cref="ISsoConfigurationReader"/> mock for the only sake of unit testing.
		/// </para>
		/// </remarks>
		[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
		public static ISsoConfigurationReader Instance { get; internal set; } = new SsoConfigurationReader();

		private SsoConfigurationReader() { }

		#region ISsoConfigurationReader Members

		public object Read(string affiliateApplicationName, string configPropertyName)
		{
			var affiliateApplication = AffiliateApplicationCache.Instance[affiliateApplicationName];
			var configStore = affiliateApplication.ConfigStores.Default
				?? throw new InvalidOperationException(
					$"The {nameof(AffiliateApplication)} '{affiliateApplication.Name}' is probably not managed by BizTalk.Factory and has not default {nameof(ConfigStore)}.");
			return configStore.AgedLessThan(MaxAge).Properties.TryGetValue(configPropertyName, out var value) && value != null
				? value
				: throw new ArgumentException(
					$"The {nameof(AffiliateApplication)} '{affiliateApplicationName}' does not provide a value for the configuration property '{configPropertyName}' in its default {nameof(ConfigStore)}.",
					nameof(configPropertyName));
		}

		#endregion

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		// ReSharper disable once MemberCanBePrivate.Global, relied upon by unit tests
		internal static TimeSpan MaxAge = TimeSpan.FromSeconds(60);
	}
}
