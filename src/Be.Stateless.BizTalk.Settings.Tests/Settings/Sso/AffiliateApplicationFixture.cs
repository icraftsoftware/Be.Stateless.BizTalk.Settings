#region Copyright & License

// Copyright © 2012 - 2021 François Chabot
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
using System.Linq;
using FluentAssertions;
using Microsoft.EnterpriseSingleSignOn.Interop;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Be.Stateless.BizTalk.Settings.Sso
{
	[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
	public class AffiliateApplicationFixture : IDisposable
	{
		#region Setup/Teardown

		public AffiliateApplicationFixture()
		{
			_affiliateApplication = AffiliateApplication.FindByName(nameof(AffiliateApplicationFixture)) ?? AffiliateApplication.Create(nameof(AffiliateApplicationFixture));
		}

		public void Dispose()
		{
			_affiliateApplication.Delete();
		}

		#endregion

		[Fact]
		public void AffiliateApplicationCreatedByBizTalkFactoryContainsDefaultConfigStore()
		{
			var affiliateApplication = AffiliateApplication.FindByName(_affiliateApplication.Name);
			affiliateApplication.ConfigStores.Should().NotBeEmpty();
			affiliateApplication.ConfigStores.Default.Should().NotBeNull();
			affiliateApplication.ConfigStores.Should().OnlyContain(kvp => kvp.Key == ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
		}

		[Fact]
		public void AffiliateApplicationCreatedByBizTalkFactoryContainsTwoFields()
		{
			new ISSOMapper2().GetFieldInfo(_affiliateApplication.Name, out var labels, out _);
			labels.Should().BeEquivalentTo(AffiliateApplication.DEFAULT_CONTACT_INFO, AffiliateApplication.DEFAULT_SETTINGS_KEY);
		}

		[Fact]
		public void AffiliateApplicationNotCreatedByBizTalkFactoryDoesNotContainDefaultConfigStore()
		{
			var affiliateApplication = AffiliateApplication.FindByContact(AffiliateApplication.ANY_CONTACT_INFO).First(a => a.Contact != AffiliateApplication.DEFAULT_CONTACT_INFO);
			affiliateApplication.ConfigStores.Should().NotBeEmpty();
			affiliateApplication.ConfigStores.Default.Should().BeNull();
			affiliateApplication.ConfigStores.Should().OnlyContain(kvp => kvp.Key != ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
		}

		[Fact]
		public void Create()
		{
			const string name = nameof(AffiliateApplicationFixture) + ".Create";
			try
			{
				Invoking(() => AffiliateApplication.Create(name)).Should().NotThrow();
				AffiliateApplication.FindByName(name).Should().NotBeNull();
			}
			finally
			{
				AffiliateApplication.FindByName(name)?.Delete();
			}
		}

		[Fact]
		public void CreateThrowsIfAlreadyExists()
		{
			Invoking(() => AffiliateApplication.Create(_affiliateApplication.Name))
				.Should().Throw<ArgumentException>()
				.WithMessage($"{nameof(AffiliateApplication)} '{_affiliateApplication.Name}' already exists and cannot be duplicated.*");
		}

		[Fact]
		public void Delete()
		{
			const string name = nameof(AffiliateApplicationFixture) + ".Delete";
			try
			{
				var affiliateApplication = AffiliateApplication.Create(name);
				Invoking(() => affiliateApplication.Delete()).Should().NotThrow();
				AffiliateApplication.FindByName(name).Should().BeNull();
			}
			finally
			{
				AffiliateApplication.FindByName(name)?.Delete();
			}
		}

		[Fact]
		public void DeleteThrowsIfBizTalkFactoryDefaultContactDoesNotHaveOwnership()
		{
			var affiliateApplication = AffiliateApplication.FindByContact(AffiliateApplication.ANY_CONTACT_INFO)
				.First(a => !a.Name.StartsWith(nameof(AffiliateApplicationFixture)) && a.Contact != AffiliateApplication.DEFAULT_CONTACT_INFO);
			Invoking(() => affiliateApplication.Delete())
				.Should().Throw<InvalidOperationException>()
				.WithMessage(
					$"To prevent any destructive effects, BizTalk.Factory will not delete an {nameof(AffiliateApplication)} that it has not created or that has other {nameof(ConfigStore)}s than the default one.");
		}

		[Fact]
		public void DeleteThrowsIfHasMultipleConfigStores()
		{
			const string name = nameof(AffiliateApplicationFixture) + ".MultipleConfigStores";
			try
			{
				var affiliateApplication = AffiliateApplication.FindByName(name) ?? AffiliateApplication.Create(name);

				var defaultConfigStore = new ConfigStore(affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				defaultConfigStore.Properties["key1"] = "value1";
				Invoking(() => defaultConfigStore.Save()).Should().NotThrow();

				var otherConfigStore = new ConfigStore.ConfigStoreProperties(affiliateApplication.Name, Guid.NewGuid().ToString("B"));
				otherConfigStore.Properties["key2"] = "value2";
				Invoking(() => otherConfigStore.Save()).Should().NotThrow();

				AffiliateApplication.FindByName(name).ConfigStores.Should().HaveCount(2);

				Invoking(() => affiliateApplication.Delete())
					.Should().Throw<InvalidOperationException>()
					.WithMessage(
						$"To prevent any destructive effects, BizTalk.Factory will not delete an {nameof(AffiliateApplication)} that it has not created or that has other {nameof(ConfigStore)}s than the default one.");

				otherConfigStore.Delete();
			}
			finally
			{
				AffiliateApplication.FindByName(name)?.Delete();
			}
		}

		[Fact]
		public void FindByAnyContact()
		{
			var affiliateApplications = AffiliateApplication.FindByContact(AffiliateApplication.ANY_CONTACT_INFO).ToArray();
			affiliateApplications.Should().NotBeEmpty();
			affiliateApplications.Where(a => a == null).Should().BeEmpty();
		}

		[Fact]
		public void FindByDefaultContact()
		{
			var affiliateApplications = AffiliateApplication.FindByContact().ToArray();
			affiliateApplications.Should().HaveCountGreaterOrEqualTo(1);
			affiliateApplications.Should().ContainEquivalentOf(_affiliateApplication);
		}

		[Fact]
		public void FindByName()
		{
			var store = AffiliateApplication.FindByName(_affiliateApplication.Name);
			store.Should().BeEquivalentTo(_affiliateApplication);
		}

		private readonly AffiliateApplication _affiliateApplication;
	}
}
