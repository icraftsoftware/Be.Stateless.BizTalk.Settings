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
using Be.Stateless.BizTalk.Settings.Sso;

namespace Be.Stateless.BizTalk.Settings
{
	/// <summary>
	/// Client interface to read individual configuration properties from an <see cref="AffiliateApplication"/>'s default <see
	/// cref="ConfigStore"/> stored by the Enterprise Single Sign-On (SSO) server database.
	/// </summary>
	/// <remarks>
	/// This interface essentially exists to provide the ability to inject an <see cref="ISsoConfigurationReader"/> mock the sake
	/// of unit testing; see <see cref="SsoConfigurationReader.Instance">SsoConfigurationReader.Instance</see>.
	/// </remarks>
	/// <seealso cref="AffiliateApplication.ConfigStores">AffiliateApplication.ConfigStores</seealso>
	/// <seealso cref="ConfigStoreCollection.Default">ConfigStoreCollection.Default</seealso>
	/// <seealso cref="SsoConfigurationReader.Instance">SsoConfigurationReader.Instance</seealso>
	public interface ISsoConfigurationReader
	{
		/// <summary>
		/// Read the value of the individual configuration property <paramref name="configPropertyName"/> for the <see
		/// cref="AffiliateApplication"/> <paramref name="affiliateApplicationName"/> from its default <see cref="ConfigStore"/>.
		/// </summary>
		/// <param name="affiliateApplicationName">
		/// The name of the <see cref="AffiliateApplication"/> for which to return the value of the <paramref
		/// name="configPropertyName"/> configuration property from its default <see cref="ConfigStore"/>.
		/// </param>
		/// <param name="configPropertyName">
		/// The name of the configuration property to return the value of.
		/// </param>
		/// <returns>
		/// The value of the configuration property.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// The configuration property has not been defined for the <see cref="AffiliateApplication"/> named <paramref
		/// name="affiliateApplicationName"/>.
		/// </exception>
		object Read(string affiliateApplicationName, string configPropertyName);
	}
}
