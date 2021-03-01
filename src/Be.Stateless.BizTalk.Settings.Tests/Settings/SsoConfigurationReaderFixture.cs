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
using System.Linq;
using System.Threading;
using Be.Stateless.BizTalk.Settings.Sso;
using FluentAssertions;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Be.Stateless.BizTalk.Settings
{
	public class SsoConfigurationReaderFixture
	{
		[Fact]
		public void ReadEnsuresConfigStoreIsAlwaysFresh()
		{
			const string name = nameof(SsoConfigurationReaderFixture) + ".fresh";
			try
			{
				var application = AffiliateApplication.FindByName(name) ?? AffiliateApplication.Create(name);
				application.ConfigStores.Default.Properties["Key1"] = "Value1";
				application.ConfigStores.Default.Save();

				SsoConfigurationReader.Instance.Read(name, "Key1").Should().Be("Value1");

				application.ConfigStores.Default.Properties["Key1"] = "Value2";
				application.ConfigStores.Default.Save();

				// reads stalled property value
				SsoConfigurationReader.Instance.Read(name, "Key1").Should().Be("Value1");

				SsoConfigurationReader.MaxAge = TimeSpan.FromSeconds(1);
				Thread.Sleep(TimeSpan.FromSeconds(1));

				// reads fresh property value
				SsoConfigurationReader.Instance.Read(name, "Key1").Should().Be("Value2");
			}
			finally
			{
				SsoConfigurationReader.MaxAge = TimeSpan.FromSeconds(60);
				AffiliateApplication.FindByName(name)?.Delete();
			}
		}

		[Fact]
		public void ReadFromSeveralAffiliateApplications()
		{
			const string name1 = nameof(SsoConfigurationReaderFixture) + ".one";
			const string name2 = nameof(SsoConfigurationReaderFixture) + ".two";
			try
			{
				var application1 = AffiliateApplication.FindByName(name1) ?? AffiliateApplication.Create(name1);
				application1.ConfigStores.Default.Properties["Key1"] = "Value1";
				application1.ConfigStores.Default.Save();

				var application2 = AffiliateApplication.FindByName(name2) ?? AffiliateApplication.Create(name2);
				application2.ConfigStores.Default.Properties["Key2"] = "Value2";
				application2.ConfigStores.Default.Save();

				SsoConfigurationReader.Instance.Read(name1, "Key1").Should().Be("Value1");
				SsoConfigurationReader.Instance.Read(name2, "Key2").Should().Be("Value2");
			}
			finally
			{
				AffiliateApplication.FindByName(name1)?.Delete();
				AffiliateApplication.FindByName(name2)?.Delete();
			}
		}

		[Fact]
		public void ReadThrowsIfAffiliateApplicationDoesNotExist()
		{
			const string name = "NonexistentApplication";
			Invoking(() => SsoConfigurationReader.Instance.Read(name, "property"))
				.Should().Throw<InvalidOperationException>()
				.WithMessage($"{nameof(AffiliateApplication)} '{name}' does not exist.");
		}

		[Fact]
		public void ReadThrowsIfAffiliateApplicationHasNoDefaultStore()
		{
			var application = AffiliateApplication.FindByContact(AffiliateApplication.ANY_CONTACT_INFO).First();
			Invoking(() => SsoConfigurationReader.Instance.Read(application.Name, "property"))
				.Should().Throw<InvalidOperationException>()
				.WithMessage(
					$"The {nameof(AffiliateApplication)} '{application.Name}' is probably not managed by BizTalk.Factory and has not default {nameof(ConfigStore)}.");
		}

		[Fact]
		public void ReadThrowsIfPropertyDoesNotExist()
		{
			const string name = nameof(SsoConfigurationReaderFixture) + ".miss";
			try
			{
				var application = AffiliateApplication.FindByName(name) ?? AffiliateApplication.Create(name);
				application.ConfigStores.Default.Properties["Key1"] = "Value1";
				application.ConfigStores.Default.Save();

				Invoking(() => SsoConfigurationReader.Instance.Read(name, "Key2"))
					.Should().Throw<ArgumentException>()
					.WithMessage(
						$"The {nameof(AffiliateApplication)} '{name}' does not provide a value for the configuration property 'Key2' in its default {nameof(ConfigStore)}.*");
			}
			finally
			{
				AffiliateApplication.FindByName(name)?.Delete();
			}
		}
	}
}
