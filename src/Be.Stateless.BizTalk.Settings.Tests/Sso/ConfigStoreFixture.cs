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
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Be.Stateless.BizTalk.Settings.Sso
{
	[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
	public class ConfigStoreFixture : IDisposable
	{
		[Fact]
		public void ConfigStoreIsReloadedAfter60Seconds()
		{
			true.Should().BeFalse("TODO");
			// TODO ?? what if property dictionary is dirty ??
		}

		[Fact]
		public void DefaultConfigStoreIsInitiallyEmpty()
		{
			var configStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
			configStore.Properties.Should().BeEmpty();
		}

		[Fact]
		public void DeleteExistentDefaultConfigStoreDoesNotThrow()
		{
			var configStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
			configStore.Properties.Should().BeEmpty();
			configStore.Properties["Key1"] = "Value1";
			configStore.Save();
			Action act = () => configStore.Delete();
			act.Should().NotThrow();
		}

		[Fact]
		public void DeleteExistentNonDefaultConfigStoreThrows()
		{
			var affiliateApplication = AffiliateApplication.FindByContact(AffiliateApplication.ANY_CONTACT_INFO)
				.First(s => s.Contact != AffiliateApplication.DEFAULT_CONTACT_INFO);
			affiliateApplication.ConfigStores.Should().NotBeEmpty();
			var configStore = affiliateApplication.ConfigStores.Values.First();

			Action act = () => configStore.Delete();

			act.Should()
				.Throw<InvalidOperationException>()
				.WithMessage("Cannot delete a ConfigStore other than the default one.");
		}

		[Fact]
		public void DeleteNonexistentDefaultConfigStoreDoesNotThrow()
		{
			var configStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
			Action act = () => configStore.Delete();
			act.Should().NotThrow();
		}

		[Fact]
		public void DeleteNonexistentNonDefaultConfigStoreThrows()
		{
			var configStore = new ConfigStore(_affiliateApplication.Name, Guid.NewGuid().ToString("B"));
			Action act = () => configStore.Delete();
			act.Should()
				.Throw<InvalidOperationException>()
				.WithMessage("Cannot delete a ConfigStore other than the default one.");
		}

		[Fact]
		public void LoadExistentDefaultConfigStore()
		{
			try
			{
				var newConfigStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				newConfigStore.Properties["Key1"] = "Value1";
				newConfigStore.Properties["Key2"] = "Value2";
				newConfigStore.Save();

				var existentConfigStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				existentConfigStore.Properties.Should().NotBeEmpty();
			}
			finally
			{
				new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER).Delete();
			}
		}

		[Fact]
		public void LoadExistentNonDefaultConfigStore()
		{
			var affiliateApplication = AffiliateApplication.FindByContact(AffiliateApplication.ANY_CONTACT_INFO)
				.First(s => s.Contact != AffiliateApplication.DEFAULT_CONTACT_INFO);
			affiliateApplication.ConfigStores.Should().NotBeEmpty();
			var existentConfigStore = affiliateApplication.ConfigStores.Values.First();
			existentConfigStore.Properties.Should().NotBeEmpty();
		}

		[Fact]
		public void LoadNonexistentDefaultConfigStore()
		{
			var configStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
			configStore.Properties.Should().BeEmpty();
		}

		[Fact]
		public void LoadNonexistentNonDefaultConfigStore()
		{
			var configStore = new ConfigStore(_affiliateApplication.Name, Guid.NewGuid().ToString("B"));
			configStore.Properties.Should().BeEmpty();
		}

		[Fact]
		public void NonDefaultConfigStoreIsInitiallyEmpty()
		{
			var configStore = new ConfigStore(_affiliateApplication.Name, Guid.NewGuid().ToString("B"));
			configStore.Properties.Should().BeEmpty();
		}

		[Fact]
		public void SaveExistentNonDefaultConfigStoreThrows()
		{
			var affiliateApplication = AffiliateApplication.FindByContact(AffiliateApplication.ANY_CONTACT_INFO)
				.First(s => s.Contact != AffiliateApplication.DEFAULT_CONTACT_INFO);
			affiliateApplication.ConfigStores.Should().NotBeEmpty();
			var configStore = affiliateApplication.ConfigStores.Values.First();
			configStore.Properties["Key1"] = "Value1";

			Action act = () => configStore.Save();

			act.Should()
				.Throw<InvalidOperationException>()
				.WithMessage("Cannot save or overwrite the properties of a ConfigStore other than the default one.");
		}

		[Fact]
		public void SaveNonexistentDefaultConfigStore()
		{
			try
			{
				var configStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				configStore.Properties.Should().BeEmpty();
				configStore.Properties["Key1"] = "Value1";
				configStore.Properties["Key2"] = "Value2";
				configStore.Save();

				var reloadConfigStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				reloadConfigStore.Properties.Should().BeEquivalentTo(configStore.Properties);
			}
			finally
			{
				var configStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				configStore.Delete();
			}
		}

		[Fact]
		public void SaveNonexistentNonDefaultConfigStoreThrows()
		{
			var configStore = new ConfigStore(_affiliateApplication.Name, Guid.NewGuid().ToString("B"));
			Action act = () => configStore.Save();
			act.Should()
				.Throw<InvalidOperationException>()
				.WithMessage("Cannot save or overwrite the properties of a ConfigStore other than the default one.");
		}

		[Fact]
		public void UpdateExistentDefaultConfigStoreWithNewProperty()
		{
			try
			{
				var newConfigStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				newConfigStore.Properties.Should().BeEmpty();
				newConfigStore.Properties["Key1"] = "Value1";
				newConfigStore.Properties["Key2"] = "Value2";
				newConfigStore.Save();

				var existentConfigStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				existentConfigStore.Properties.Should().BeEquivalentTo(newConfigStore.Properties);
				existentConfigStore.Properties["Key1"] = "Value3";
				existentConfigStore.Properties["Key2"] = "Value4";
				existentConfigStore.Properties["Key9"] = "Value9";
				existentConfigStore.Save();

				var reloadConfigStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				reloadConfigStore.Properties.Should().NotContainValues("Value1", "Value2");
				reloadConfigStore.Properties.Should().ContainKeys("Key1", "Key2", "Key9");
				reloadConfigStore.Properties.Should().BeEquivalentTo(existentConfigStore.Properties);
			}
			finally
			{
				var configStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				configStore.Delete();
			}
		}

		[Fact]
		public void UpdateExistentDefaultConfigStoreWithValueUpdates()
		{
			try
			{
				var newConfigStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				newConfigStore.Properties.Should().BeEmpty();
				newConfigStore.Properties["Key1"] = "Value1";
				newConfigStore.Properties["Key2"] = "Value2";
				newConfigStore.Save();

				var existentConfigStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				existentConfigStore.Properties.Should().BeEquivalentTo(newConfigStore.Properties);
				existentConfigStore.Properties["Key1"] = "Value3";
				existentConfigStore.Properties["Key2"] = "Value4";
				existentConfigStore.Save();

				var reloadConfigStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				reloadConfigStore.Properties.Should().NotContainValues("Value1", "Value2");
				reloadConfigStore.Properties.Should().BeEquivalentTo(existentConfigStore.Properties);
			}
			finally
			{
				var configStore = new ConfigStore(_affiliateApplication.Name, ConfigStoreCollection.DEFAULT_CONFIG_STORE_IDENTIFIER);
				configStore.Delete();
			}
		}

		public ConfigStoreFixture()
		{
			_affiliateApplication = AffiliateApplication.FindByName(nameof(ConfigStoreFixture)) ?? AffiliateApplication.Create(nameof(ConfigStoreFixture));
		}

		public void Dispose()
		{
			_affiliateApplication.Delete();
		}

		private readonly AffiliateApplication _affiliateApplication;
	}
}
