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
using Be.Stateless.Extensions;
using Microsoft.EnterpriseSingleSignOn.Interop;

namespace Be.Stateless.BizTalk.Settings.Sso
{
	/// <summary>
	/// Manages the <see cref="AffiliateApplication"/>s in the Enterprise Single Sign-On (SSO) server database.
	/// </summary>
	public class AffiliateApplication
	{
		/// <summary>
		/// Creates an <see cref="AffiliateApplication"/> in the Enterprise Single Sign-On (SSO) server database.
		/// </summary>
		/// <param name="name">
		/// The Application name; it cannot be NULL, an empty string, or contain spaces.
		/// </param>
		/// <param name="userGroup">
		/// The Application Users group name. It defaults to "BizTalk Application Users".
		/// </param>
		/// <param name="administratorGroup">
		/// The Application Administrators group name. It defaults to "BizTalk Server Administrators".
		/// </param>
		/// <param name="description">
		/// The Application description.
		/// </param>
		/// <returns>
		/// The <seealso cref="AffiliateApplication"/> created in the Enterprise Single Sign-On (SSO) server database.
		/// </returns>
		/// <remarks>
		/// Application names are not case-sensitive, but case will be preserved.
		/// </remarks>
		/// <seealso href="https://docs.microsoft.com/en-us/biztalk/core/how-to-create-and-describe-an-application-to-single-sign-on">How to Create and Describe an Application to Single Sign-On</seealso>
		/// <seealso href="https://github.com/MicrosoftDocs/biztalk-docs/blob/master/technical-reference/issoadmin-createapplication-method.md">ISSOAdmin.CreateApplication Method</seealso>
		[SuppressMessage("ReSharper", "CommentTypo")]
		public static AffiliateApplication Create(
			string name,
			string userGroup = DEFAULT_USER_GROUP_NAME,
			string administratorGroup = DEFAULT_ADMINISTRATOR_GROUP_NAME,
			string description = null
		)
		{
			if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));
			if (name.Contains(' ')) throw new ArgumentException("Name cannot contain spaces.", nameof(name));
			if (FindByName(name) != null) throw new ArgumentException($"{nameof(AffiliateApplication)} '{name}' already exists and cannot be duplicated.", nameof(name));

			var application = new AffiliateApplication {
				Name = name,
				AdministratorGroup = administratorGroup ?? DEFAULT_ADMINISTRATOR_GROUP_NAME,
				Contact = DEFAULT_CONTACT_INFO,
				Description = description ?? $"{name} Configuration Store",
				UserGroup = userGroup ?? DEFAULT_USER_GROUP_NAME
			};

			var ssoAdmin = new ISSOAdmin();
			ssoAdmin.CreateApplication(
				application.Name,
				application.Description,
				application.Contact,
				application.UserGroup,
				application.AdministratorGroup,
				SSOFlag.SSO_FLAG_APP_CONFIG_STORE | SSOFlag.SSO_FLAG_APP_ALLOW_LOCAL | SSOFlag.SSO_FLAG_SSO_WINDOWS_TO_EXTERNAL,
				2 /* number of fields to be created */);
			ssoAdmin.CreateFieldInfo(name, application.Contact, SSOFlag.SSO_FLAG_NONE);
			ssoAdmin.CreateFieldInfo(name, DEFAULT_SETTINGS_KEY, SSOFlag.SSO_FLAG_NONE);
			ssoAdmin.UpdateApplication(name, null, null, null, null, SSOFlag.SSO_FLAG_ENABLED, SSOFlag.SSO_FLAG_ENABLED);

			return application;
		}

		/// <summary>
		/// Returns all the <see cref="AffiliateApplication"/>s which are associated to a given <paramref name="contact"/> and
		/// that are currently deployed in the Enterprise Single Sign-On (SSO) server database.
		/// </summary>
		/// <param name="contact">
		/// Name of the <see cref="AffiliateApplication"/>s' contact that will be used as filter. It defaults to <see
		/// cref="DEFAULT_CONTACT_INFO"/>.
		/// </param>
		/// <returns>
		/// The <see cref="AffiliateApplication"/>s currently deployed in the Enterprise Single Sign-On (SSO) server database.
		/// </returns>
		/// <remarks>
		/// By default, only the <see cref="AffiliateApplication"/>s that have been associated to <see
		/// cref="DEFAULT_CONTACT_INFO"/> will be retrieved. If <paramref name="contact"/> is <see cref="ANY_CONTACT_INFO"/> all
		/// the <see cref="AffiliateApplication"/>s will be returned, regardless of whether they have been created by
		/// BizTalk.Factory's <see cref="AffiliateApplication"/>.<see cref="Create"/> factory method; that is to say, regardless
		/// of their <see cref="Contact"/>.
		/// </remarks>
		[SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")]
		[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
		public static IEnumerable<AffiliateApplication> FindByContact(string contact = DEFAULT_CONTACT_INFO)
		{
			if (contact.IsNullOrEmpty()) throw new ArgumentNullException(nameof(contact));
			var mapper = new ISSOMapper2();
			// https://docs.microsoft.com/en-us/biztalk/core/how-to-change-the-behavior-of-a-single-sign-on-interface
			var propBag = (IPropertyBag) mapper;
			object appFilterFlags = (uint) (AffiliateApplicationType.ConfigStore | AffiliateApplicationType.All);
			propBag.Write("AppFilterFlags", ref appFilterFlags);
			object appFilterFlagMask = (uint) SSOFlag.SSO_FLAG_APP_FILTER_BY_TYPE;
			propBag.Write("AppFilterFlagMask", ref appFilterFlagMask);

			mapper.GetApplications2(out var applications, out var descriptions, out var contacts, out var userAccounts, out var adminAccounts, out _);
			return Enumerable.Range(0, applications.Length)
				.Where(i => contact == ANY_CONTACT_INFO || contacts[i].Equals(contact, StringComparison.OrdinalIgnoreCase))
				.Select(
					i => new AffiliateApplication {
						Name = applications[i],
						Description = descriptions[i],
						Contact = contacts[i],
						AdministratorGroup = adminAccounts[i],
						UserGroup = userAccounts[i]
					});
		}

		/// <summary>
		/// Finds and returns an <see cref="AffiliateApplication"/> by name in the Enterprise Single Sign-On (SSO) server
		/// database.
		/// </summary>
		/// <param name="name">
		/// The name of the <see cref="AffiliateApplication"/> to find in Enterprise Single Sign-On (SSO) server database.
		/// </param>
		/// <returns>
		/// The <see cref="AffiliateApplication"/> currently deployed in the Enterprise Single Sign-On (SSO) server database.
		/// <c>null</c> if the <see cref="AffiliateApplication"/> does not exist in Enterprise Single Sign-On (SSO) server
		/// database.
		/// </returns>
		public static AffiliateApplication FindByName(string name)
		{
			try
			{
				var ssoAdmin = new ISSOAdmin();
				ssoAdmin.GetApplicationInfo(name, out var description, out var contact, out var userGroupName, out var adminGroupName, out _, out _);
				return new AffiliateApplication { Name = name, Contact = contact, Description = description, AdministratorGroup = adminGroupName, UserGroup = userGroupName };
			}
			catch (COMException exception)
			{
				if ((uint) exception.ErrorCode == (uint) HResult.ErrorApplicationNonExistent) return null;
				throw;
			}
		}

		private AffiliateApplication()
		{
			// rely on lazy initialization to provide thread-safe instantiation
			_lazyConfigStoreCollection = new Lazy<ConfigStoreCollection>(() => new ConfigStoreCollection(this));
		}

		/// <summary>
		/// The application Administrators group name.
		/// </summary>
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
		public string AdministratorGroup { get; private set; }

		/// <summary>
		/// The application's <see cref="ConfigStore"/>s.
		/// </summary>
		public ConfigStoreCollection ConfigStores => _lazyConfigStoreCollection.Value;

		/// <summary>
		/// The application contact information.
		/// </summary>
		public string Contact { get; private set; }

		/// <summary>
		/// The application description.
		/// </summary>
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
		public string Description { get; private set; }

		/// <summary>
		/// The application name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The application Users group name.
		/// </summary>
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
		public string UserGroup { get; private set; }

		/// <summary>
		/// Determines whether this <see cref="AffiliateApplication"/> has been created by <b>BizTalk.Factory</b>, i.e by this
		/// class <see cref="Create"/> factory method.
		/// </summary>
		/// <remarks>
		/// This essentially checks whether this <see cref="AffiliateApplication"/>.<see cref="Contact"/> is <see
		/// cref="DEFAULT_CONTACT_INFO"/>.
		/// </remarks>
		/// <seealso cref="Create"/>
		internal bool HasOwnership => Contact == DEFAULT_CONTACT_INFO;

		/// <summary>
		/// Deletes an <see cref="AffiliateApplication"/> from the Enterprise Single Sign-On (SSO) server database.
		/// </summary>
		public void Delete()
		{
			if (!HasOwnership || _lazyConfigStoreCollection.Value.ContainsForeignConfigStores)
				throw new InvalidOperationException(
					$"To prevent any destructive effects, BizTalk.Factory will not delete an {nameof(AffiliateApplication)} "
					+ $"that it has not created or that has other {nameof(ConfigStore)}s than the default one.");
			var ssoAdmin = new ISSOAdmin();
			ssoAdmin.DeleteApplication(Name);
		}

		[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
		public const string ANY_CONTACT_INFO = "*";

		internal const string DEFAULT_CONTACT_INFO = "icraftsoftware@stateless.be";
		internal const string DEFAULT_SETTINGS_KEY = "settings";
		private const string DEFAULT_ADMINISTRATOR_GROUP_NAME = "BizTalk Server Administrators";
		private const string DEFAULT_USER_GROUP_NAME = "BizTalk Application Users";
		private readonly Lazy<ConfigStoreCollection> _lazyConfigStoreCollection;
	}
}
